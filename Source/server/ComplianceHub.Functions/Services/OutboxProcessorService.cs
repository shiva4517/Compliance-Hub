using System.Data;
using System.Text.Json;
using ComplianceHub.Functions.Models;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace ComplianceHub.Functions.Services;

public class OutboxProcessorService(
    NpgsqlDataSource regulationsDs,
    [FromKeyedServices("compliancehub")] NpgsqlDataSource ds,
    ILogger<OutboxProcessorService> logger)
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    public async Task ProcessAsync(CancellationToken ct)
    {
        await using var regulationsDb = await regulationsDs.OpenConnectionAsync(ct);
        await using var portalDb = await ds.OpenConnectionAsync(ct);

        var events = (await regulationsDb.QueryAsync<OutboxEventRow>(
            @"SELECT ""Id"",""EventType"",""Payload"",""RetryCount""
              FROM ""OutboxEvents""
              WHERE ""ProcessedAt"" IS NULL AND ""RetryCount"" < 5 AND ""IsDeleted""=false
              ORDER BY ""CreatedAt"" LIMIT 100"))
            .ToList();

        if (events.Count == 0) { logger.LogInformation("No pending outbox events"); return; }

        logger.LogInformation("Processing {Count} outbox events", events.Count);

        foreach (var ev in events)
        {
            try
            {
                await ProcessEventAsync(ev, regulationsDb, portalDb, ct);
                await regulationsDb.ExecuteAsync(
                    @"UPDATE ""OutboxEvents"" SET ""ProcessedAt""=@now, ""UpdatedAt""=@now WHERE ""Id""=@id",
                    new { now = DateTime.UtcNow, id = ev.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox event {Id}", ev.Id);
                await regulationsDb.ExecuteAsync(
                    @"UPDATE ""OutboxEvents"" SET ""RetryCount""=""RetryCount""+1, ""UpdatedAt""=@now WHERE ""Id""=@id",
                    new { now = DateTime.UtcNow, id = ev.Id });
            }
        }
    }

    private async Task ProcessEventAsync(OutboxEventRow ev, NpgsqlConnection regulationsDb, NpgsqlConnection portalDb, CancellationToken ct)
    {
        if (ev.EventType != "RegulationChanged") return;

        var payload = JsonSerializer.Deserialize<RegulationChangedPayload>(ev.Payload, _json);
        if (payload is null) return;

        // ── 1. Load regulation + its full hierarchy from the Regulations DB ────
        var reg = await LoadRegulationAsync(payload.RegulationId, regulationsDb);
        if (reg is null)
        {
            logger.LogWarning("OutboxEvent {Id} references missing Regulation {RegId}", ev.Id, payload.RegulationId);
            return;
        }

        // ── 2. Load previous-version snapshot for diff fields ──────────────────
        var snapshot = await LoadPreviousSnapshotAsync(reg.RegulationId, payload.Version, regulationsDb);

        // ── 3. Begin portal-DB transaction ─────────────────────────────────────
        await using var portalTx = await portalDb.BeginTransactionAsync(ct);
        try
        {
            // ── 4. Insert ChangeNotice (audit row for the change) ──────────────
            await InsertChangeNoticeAsync(reg, snapshot, portalDb, portalTx);

            // ── 5. Match subscriptions across all 6 hierarchy levels ───────────
            var matchedSubs = await MatchSubscriptionsAsync(reg, portalDb, portalTx);

            if (matchedSubs.Count == 0)
            {
                await portalTx.CommitAsync(ct);
                logger.LogInformation(
                    "ChangeNotice written for section {Section} (v{Version}). No matching active subscriptions.",
                    reg.SectionNumber, payload.Version);
                return;
            }

            // ── 6. Load customers for matched subs ─────────────────────────────
            var customerIds = matchedSubs.Select(s => s.CustomerId).Distinct().ToArray();
            var customers = (await portalDb.QueryAsync<CustomerWithCompany>(
                @"SELECT ""Id"",""CompanyId"",""CustomerName"",""PrimaryEmail""
                  FROM ""Customers""
                  WHERE ""Id"" = ANY(@ids) AND ""IsDeleted""=false",
                new { ids = customerIds },
                portalTx))
                .ToDictionary(c => c.Id);

            // ── 7. Build notification rows and bulk insert ─────────────────────
            var notifications = BuildNotifications(reg, snapshot, matchedSubs, customers);

            if (notifications.Any())
                await InsertNotificationHistoryAsync(notifications, portalDb, portalTx);

            await portalTx.CommitAsync(ct);
            logger.LogInformation(
                "Processed outbox event for section {Section} (v{Version}). Matched {SubCount} subscription(s), wrote {NotifCount} notification(s).",
                reg.SectionNumber, payload.Version, matchedSubs.Count, notifications.Count);
        }
        catch
        {
            await portalTx.RollbackAsync(ct);
            throw;
        }
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Data loaders
    // ───────────────────────────────────────────────────────────────────────────

    private static async Task<RegulationWithHierarchy?> LoadRegulationAsync(Guid regulationId, NpgsqlConnection regulationsDb)
    {
        return await regulationsDb.QueryFirstOrDefaultAsync<RegulationWithHierarchy>(
            @"SELECT
                r.""Id""                AS RegulationId,
                r.""SectionNumber""     AS SectionNumber,
                r.""SectionName""       AS SectionName,
                r.""HtmlContent""       AS HtmlContent,
                r.""ContentHash""       AS ContentHash,
                r.""Version""           AS Version,
                r.""LastAmendedDate""   AS LastAmendedDate,
                r.""GovernmentEntityId"" AS GovernmentEntityId,
                ge.""TitleName""        AS GovernmentEntityName,
                r.""AgencyId""          AS AgencyId,
                a.""AgencyName""        AS AgencyName,
                r.""RegulationCategoryId"" AS RegulationCategoryId,
                rc.""SubchapterName""   AS RegulationCategoryName,
                r.""RegulationTypeId""  AS RegulationTypeId,
                rt.""PartName""         AS RegulationTypeName,
                r.""RegulationSubtypeId"" AS RegulationSubtypeId,
                rs.""SubpartName""      AS RegulationSubtypeName
              FROM ""Regulations"" r
              JOIN ""GovernmentEntities"" ge ON ge.""Id"" = r.""GovernmentEntityId""
              JOIN ""Agencies"" a              ON a.""Id""  = r.""AgencyId""
              JOIN ""RegulationCategories"" rc ON rc.""Id"" = r.""RegulationCategoryId""
              JOIN ""RegulationTypes"" rt      ON rt.""Id"" = r.""RegulationTypeId""
              LEFT JOIN ""RegulationSubtypes"" rs ON rs.""Id"" = r.""RegulationSubtypeId""
              WHERE r.""Id"" = @id",
            new { id = regulationId });
    }

    private static async Task<PreviousSnapshot?> LoadPreviousSnapshotAsync(Guid regulationId, int currentVersion, NpgsqlConnection regulationsDb)
    {
        if (currentVersion <= 1) return null;
        return await regulationsDb.QueryFirstOrDefaultAsync<PreviousSnapshot>(
            @"SELECT ""SectionNumber"",""HtmlContent"",""ContentHash"",""Version""
              FROM ""RegulationChangeHistory""
              WHERE ""RegulationId"" = @id AND ""Version"" = @v
              LIMIT 1",
            new { id = regulationId, v = currentVersion - 1 });
    }

    private static async Task InsertChangeNoticeAsync(RegulationWithHierarchy reg, PreviousSnapshot? snapshot, NpgsqlConnection portalDb, NpgsqlTransaction portalTx)
    {
        await portalDb.ExecuteAsync(
            @"INSERT INTO ""ChangeNotices"" (
                ""Id"",""GovernmentEntityId"",""GovernmentEntityName"",
                ""AgencyId"",""AgencyName"",
                ""RegulationCategoryId"",""RegulationCategoryName"",
                ""RegulationTypeId"",""RegulationTypeName"",
                ""RegulationSubtypeId"",""RegulationSubtypeName"",
                ""RegulationId"",""SectionNumber"",""SectionTitle"",
                ""PreviousContentHash"",""NewContentHash"",
                ""PreviousHtmlContent"",""NewHtmlContent"",
                ""PreviousAmendedDate"",""NewAmendedDate"",
                ""ArchivedVersion"",""NewVersion"",""Version"",
                ""ChangedAt"",""CreatedBy""
              ) VALUES (
                @id,@geid,@geName,
                @agencyId,@agencyName,
                @catId,@catName,
                @typeId,@typeName,
                @subtypeId,@subtypeName,
                @regId,@section,@title,
                @prevHash,@newHash,
                @prevHtml,@newHtml,
                @prevAmended,@newAmended,
                @archVer,@newVer,@version,
                @now,@createdBy
              )",
            new
            {
                id = Guid.NewGuid(),
                geid = reg.GovernmentEntityId,
                geName = reg.GovernmentEntityName,
                agencyId = reg.AgencyId,
                agencyName = reg.AgencyName,
                catId = reg.RegulationCategoryId,
                catName = reg.RegulationCategoryName,
                typeId = reg.RegulationTypeId,
                typeName = reg.RegulationTypeName,
                subtypeId = reg.RegulationSubtypeId,
                subtypeName = reg.RegulationSubtypeName,
                regId = reg.RegulationId,
                section = reg.SectionNumber,
                title = reg.SectionName,
                prevHash = snapshot?.ContentHash,
                newHash = reg.ContentHash,
                prevHtml = snapshot?.HtmlContent,
                newHtml = reg.HtmlContent,
                prevAmended = (DateOnly?)null,
                newAmended = reg.LastAmendedDate,
                archVer = reg.Version - 1,
                newVer = reg.Version,
                version = reg.Version,
                now = DateTime.UtcNow,
                createdBy = "SYSTEM",
            },
            portalTx);
    }

    private static async Task<List<SubscriptionDto>> MatchSubscriptionsAsync(RegulationWithHierarchy reg, NpgsqlConnection portalDb, NpgsqlTransaction portalTx)
    {
        // Match against every subscription level the picker can produce, using
        // the regulation's actual ancestry. SubscribingLevel is stored as text
        // in the DB ("Entity"/"Agency"/…); we compare via string here.
        return (await portalDb.QueryAsync<SubscriptionDto>(
            @"SELECT ""Id"",""CustomerId"",""GovernmentEntityId"",
                     ""AgencyId"",""RegulationCategoryId"",""RegulationTypeId"",
                     ""RegulationSubtypeId"",""RegulationId""
              FROM ""Subscriptions""
              WHERE ""IsActive""=true AND ""IsDeleted""=false AND (
                    (""SubscribingLevel"" = 'Entity'     AND ""GovernmentEntityId""   = @geid)
                 OR (""SubscribingLevel"" = 'Agency'     AND ""AgencyId""             = @aid)
                 OR (""SubscribingLevel"" = 'Category'   AND ""RegulationCategoryId"" = @cid)
                 OR (""SubscribingLevel"" = 'Type'       AND ""RegulationTypeId""     = @tid)
                 OR (""SubscribingLevel"" = 'SubType'    AND ""RegulationSubtypeId"" IS NOT NULL AND ""RegulationSubtypeId"" = @stid)
                 OR (""SubscribingLevel"" = 'Regulation' AND ""RegulationId""         = @rid)
              )",
            new
            {
                geid = reg.GovernmentEntityId,
                aid = reg.AgencyId,
                cid = reg.RegulationCategoryId,
                tid = reg.RegulationTypeId,
                stid = reg.RegulationSubtypeId,
                rid = reg.RegulationId,
            },
            portalTx)).ToList();
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Build + bulk-insert NotificationHistory rows
    // ───────────────────────────────────────────────────────────────────────────

    private static List<NotificationHistoryDto> BuildNotifications(
        RegulationWithHierarchy reg,
        PreviousSnapshot? snapshot,
        IReadOnlyList<SubscriptionDto> matchedSubs,
        IReadOnlyDictionary<Guid, CustomerWithCompany> customers)
    {
        var now = DateTime.UtcNow;
        var subject = $"Regulation Change: § {reg.SectionNumber} — {reg.SectionName}";
        var body =
            $"Section § {reg.SectionNumber} ({reg.SectionName}) in {reg.GovernmentEntityName} has been updated to version {reg.Version}." +
            (snapshot is not null ? $" Previous version: v{snapshot.Version}." : "");

        var list = new List<NotificationHistoryDto>(matchedSubs.Count);
        foreach (var sub in matchedSubs)
        {
            if (!customers.TryGetValue(sub.CustomerId, out var customer)) continue;

            list.Add(new NotificationHistoryDto
            {
                Id = Guid.NewGuid(),
                CompanyId = customer.CompanyId,
                CustomerId = customer.Id,
                SubscriptionId = sub.Id,
                GovernmentEntityId = reg.GovernmentEntityId,
                GovernmentEntityName = reg.GovernmentEntityName,
                AgencyId = reg.AgencyId,
                AgencyName = reg.AgencyName,
                RegulationCategoryId = reg.RegulationCategoryId,
                RegulationCategoryName = reg.RegulationCategoryName,
                RegulationTypeId = reg.RegulationTypeId,
                RegulationTypeName = reg.RegulationTypeName,
                RegulationSubtypeId = reg.RegulationSubtypeId,
                RegulationSubtypeName = reg.RegulationSubtypeName,
                PreviousRegulationId = reg.RegulationId,
                PreviousRegulationName = snapshot?.SectionNumber ?? reg.SectionNumber,
                PresentRegulationId = reg.RegulationId,
                PresentRegulationName = reg.SectionNumber,
                Version = reg.Version,
                Subject = subject,
                Body = body,
                SenderEmail = string.Empty,
                SenderName = "Compliance Hub",
                RecipientEmail = customer.PrimaryEmail,
                RecipientName = customer.CustomerName,
                Status = "Pending",
                IsNotified = false,
                RetryCount = 0,
                CreatedAt = now,
            });
        }
        return list;
    }

    private static async Task InsertNotificationHistoryAsync(
        IReadOnlyList<NotificationHistoryDto> notifications,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx)
    {
        // One round-trip per batch using a single multi-row INSERT via Dapper's
        // IEnumerable-parameter expansion is awkward, so use the standard
        // pattern of one parameterized INSERT per row inside the open
        // transaction. Postgres pipelining keeps this fast for the row counts
        // we expect (typically tens, occasionally hundreds).
        const string sql = @"
            INSERT INTO ""NotificationHistory"" (
                ""Id"",""CompanyId"",""CustomerId"",""SubscriptionId"",
                ""GovernmentEntityId"",""GovernmentEntityName"",
                ""AgencyId"",""AgencyName"",
                ""RegulationCategoryId"",""RegulationCategoryName"",
                ""RegulationTypeId"",""RegulationTypeName"",
                ""RegulationSubtypeId"",""RegulationSubtypeName"",
                ""PreviousRegulationId"",""PreviousRegulationName"",
                ""PresentRegulationId"",""PresentRegulationName"",
                ""Version"",
                ""Subject"",""Body"",
                ""SenderEmail"",""SenderName"",
                ""RecipientEmail"",""RecipientName"",
                ""Status"",""IsNotified"",""RetryCount"",
                ""CreatedAt""
            ) VALUES (
                @Id,@CompanyId,@CustomerId,@SubscriptionId,
                @GovernmentEntityId,@GovernmentEntityName,
                @AgencyId,@AgencyName,
                @RegulationCategoryId,@RegulationCategoryName,
                @RegulationTypeId,@RegulationTypeName,
                @RegulationSubtypeId,@RegulationSubtypeName,
                @PreviousRegulationId,@PreviousRegulationName,
                @PresentRegulationId,@PresentRegulationName,
                @Version,
                @Subject,@Body,
                @SenderEmail,@SenderName,
                @RecipientEmail,@RecipientName,
                @Status,@IsNotified,@RetryCount,
                @CreatedAt
            )";

        // Dapper supports list-of-models against the same parameter shape — this
        // executes one prepared command per row in the open transaction.
        await portalDb.ExecuteAsync(sql, notifications, portalTx);
    }

    // ───────────────────────────────────────────────────────────────────────────
    // Local DTOs
    // ───────────────────────────────────────────────────────────────────────────

    private record RegulationChangedPayload(Guid RegulationId, Guid GovernmentEntityId, string SectionNumber, string ChangeType, int Version);

    private class RegulationWithHierarchy
    {
        public Guid RegulationId { get; set; }
        public string SectionNumber { get; set; } = string.Empty;
        public string SectionName { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string ContentHash { get; set; } = string.Empty;
        public int Version { get; set; }
        public DateOnly? LastAmendedDate { get; set; }

        public Guid GovernmentEntityId { get; set; }
        public string GovernmentEntityName { get; set; } = string.Empty;
        public Guid AgencyId { get; set; }
        public string AgencyName { get; set; } = string.Empty;
        public Guid RegulationCategoryId { get; set; }
        public string RegulationCategoryName { get; set; } = string.Empty;
        public Guid RegulationTypeId { get; set; }
        public string RegulationTypeName { get; set; } = string.Empty;
        public Guid? RegulationSubtypeId { get; set; }
        public string? RegulationSubtypeName { get; set; }
    }

    private record PreviousSnapshot(string SectionNumber, string HtmlContent, string ContentHash, int Version);
}
