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
        await using var db1 = await regulationsDs.OpenConnectionAsync(ct);
        await using var db2 = await ds.OpenConnectionAsync(ct);

        var events = (await db1.QueryAsync<OutboxEventRow>(
            @"SELECT ""Id"",""EventType"",""Payload"",""RetryCount"" FROM ""OutboxEvents"" WHERE ""ProcessedAt"" IS NULL AND ""RetryCount"" < 5 AND ""IsDeleted""=false ORDER BY ""CreatedAt"" LIMIT 100"))
            .ToList();

        if (events.Count == 0) { logger.LogInformation("No pending outbox events"); return; }

        logger.LogInformation("Processing {Count} outbox events", events.Count);

        foreach (var ev in events)
        {
            try
            {
                await ProcessEventAsync(ev, db2, ct);
                await db1.ExecuteAsync(
                    @"UPDATE ""OutboxEvents"" SET ""ProcessedAt""=@now, ""UpdatedAt""=@now WHERE ""Id""=@id",
                    new { now = DateTime.UtcNow, id = ev.Id });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process outbox event {Id}", ev.Id);
                await db1.ExecuteAsync(
                    @"UPDATE ""OutboxEvents"" SET ""RetryCount""=""RetryCount""+1, ""UpdatedAt""=@now WHERE ""Id""=@id",
                    new { now = DateTime.UtcNow, id = ev.Id });
            }
        }
    }

    private async Task ProcessEventAsync(OutboxEventRow ev, NpgsqlConnection db2, CancellationToken ct)
    {
        if (ev.EventType != "RegulationChanged") return;

        var payload = JsonSerializer.Deserialize<RegulationChangedPayload>(ev.Payload, _json);
        if (payload is null) return;

        await using var tx = await db2.BeginTransactionAsync(ct);
        try
        {
            var noticeId = Guid.NewGuid();
            await db2.ExecuteAsync(
                @"INSERT INTO ""ChangeNotices"" (""Id"",""GovernmentEntityId"",""RegulationId"",""Title"",""Summary"",""NoticeDate"",""IsProcessed"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@geid,@rid,@title,@summary,@date,false,@now,@now,false)",
                new
                {
                    id = noticeId,
                    geid = payload.GovernmentEntityId,
                    rid = payload.RegulationId,
                    title = $"Regulation Change: {payload.SectionNumber}",
                    summary = $"Section {payload.SectionNumber} was updated to version {payload.Version}.",
                    date = DateOnly.FromDateTime(DateTime.UtcNow),
                    now = DateTime.UtcNow
                },
                tx);

            var subscriptions = (await db2.QueryAsync<SubscriptionRow>(
                @"SELECT ""Id"",""CustomerId"",""GovernmentEntityId"",""RegulationId"",""IsActive"" FROM ""Subscriptions"" WHERE ""IsActive""=true AND ""IsDeleted""=false AND (""GovernmentEntityId""=@geid OR ""RegulationId""=@rid)",
                new { geid = payload.GovernmentEntityId, rid = payload.RegulationId },
                tx)).ToList();

            foreach (var sub in subscriptions)
            {
                await db2.ExecuteAsync(
                    @"INSERT INTO ""NotificationHistory"" (""Id"",""SubscriptionId"",""ChangeNoticeId"",""CustomerId"",""SentAt"",""IsRead"",""DeliveryStatus"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@subId,@noticeId,@customerId,@now,false,'Delivered',@now,@now,false)",
                    new { id = Guid.NewGuid(), subId = sub.Id, noticeId, customerId = sub.CustomerId, now = DateTime.UtcNow },
                    tx);
            }

            await tx.CommitAsync(ct);
            logger.LogInformation("Processed outbox event for section {Section}, notified {Count} subscribers", payload.SectionNumber, subscriptions.Count);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    private record RegulationChangedPayload(Guid RegulationId, Guid GovernmentEntityId, string SectionNumber, string ChangeType, int Version);
}
