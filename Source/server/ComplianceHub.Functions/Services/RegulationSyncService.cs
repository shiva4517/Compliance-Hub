using ComplianceHub.Functions.Models;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ComplianceHub.Functions.Services;

public class RegulationSyncService(
    EcfrClient ecfrClient,
    NpgsqlDataSource regulationsDs,
    [FromKeyedServices("compliancehub")] NpgsqlDataSource portalDs,
    ILogger<RegulationSyncService> logger)
{
    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            await using var db = await regulationsDs.OpenConnectionAsync(ct);
            await using var portalDb = await portalDs.OpenConnectionAsync(ct);
            var entities = (await db.QueryAsync<GovEntityRow>(
                @"SELECT ""Id"", ""TitleNumber"", ""TitleName"", ""IsSyncEnabled"", ""IsImported"", ""LastAmendedDate"", ""LastSyncedDate""
  FROM public.""GovernmentEntities"";"))
                .ToList();

            logger.LogInformation("Found {Count} sync-enabled entities", entities.Count);

            var titles = await ecfrClient.GetTitlesAsync(ct);

            foreach (var entity in entities)
            {
                try { await SyncEntityAsync(entity, db, titles, ct, portalDb); }
                catch (Exception ex) { logger.LogError(ex, "Sync failed for Title {TitleNumber}", entity.TitleNumber); }
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Error occurred {Message}", ex.Message.ToString()); }

    }

    private async Task SyncEntityAsync(GovEntityRow entity, NpgsqlConnection db, List<EcfrTitle> titles, CancellationToken ct, NpgsqlConnection portalDb)
    {
        var title = titles.FirstOrDefault(t => t.Number == entity.TitleNumber);
        if (title is null) { logger.LogWarning("Title {TitleNumber} not found in eCFR", entity.TitleNumber); return; }

        if (!entity.IsImported)
        {
            logger.LogInformation("Running initial import for Title {TitleNumber}", entity.TitleNumber);
            await InitialImportAsync(entity, title, db, ct);
        }
        else if (title.LatestAmendedOn.HasValue && title.LatestAmendedOn > entity.LastAmendedDateOnly)
        {
            logger.LogInformation("Running incremental sync for Title {TitleNumber}", entity.TitleNumber);
            await IncrementalSyncAsync(entity, title, db, ct,portalDb);
        }
        else
        {
            logger.LogInformation("Title {TitleNumber} is up-to-date, skipping", entity.TitleNumber);
        }

        await db.ExecuteAsync(
            @"UPDATE ""GovernmentEntities"" SET ""LastSyncedDate""=@now, ""UpdatedAt""=@now WHERE ""Id""=@id",
            new { now = DateTime.UtcNow, id = entity.Id });
    }

    private async Task InitialImportAsync(GovEntityRow entity, EcfrTitle title, NpgsqlConnection db, CancellationToken ct)
    {
        var date = title.LatestAmendedOn ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var root = await ecfrClient.GetStructureAsync(entity.TitleNumber, date, ct);

        var sections = new List<(EcfrNode Node, Guid TypeId, Guid? SubtypeId, string Path)>();
        await UpsertHierarchyAsync(entity, root, db, sections, ct);

        var batch = new List<(EcfrNode Node, Guid TypeId, Guid? SubtypeId, string Path)>();
        foreach (var section in sections)
        {
            batch.Add(section);
            if (batch.Count >= 50)
            {
                await InsertSectionBatchAsync(entity, batch, date, db, ct);
                batch.Clear();
                await Task.Delay(100, ct);
            }
        }
        if (batch.Count > 0)
            await InsertSectionBatchAsync(entity, batch, date, db, ct);

        await db.ExecuteAsync(
            @"UPDATE ""GovernmentEntities"" SET ""IsImported""=true, ""LastAmendedDate""=@date, ""UpdatedAt""=@now WHERE ""Id""=@id",
            new { date, now = DateTime.UtcNow, id = entity.Id });

        logger.LogInformation("Initial import complete for Title {TitleNumber}: {Count} sections", entity.TitleNumber, sections.Count);
    }

    private async Task UpsertHierarchyAsync(GovEntityRow entity, EcfrNode node, NpgsqlConnection db,
        List<(EcfrNode Node, Guid TypeId, Guid? SubtypeId, string Path)> sections, CancellationToken ct,
        Guid? agencyId = null, Guid? categoryId = null, Guid? typeId = null, Guid? subtypeId = null, string path = "")
    {
        var currentPath = string.IsNullOrEmpty(path) ? node.Identifier : $"{path}/{node.Identifier}";

        switch (node.Type)
        {
            case "chapter": agencyId = await UpsertAgencyAsync(entity.Id, node, db); break;
            case "subchapter": if (agencyId.HasValue) categoryId = await UpsertCategoryAsync(entity.Id, agencyId.Value, node, db); break;
            case "part": if (categoryId.HasValue) typeId = await UpsertTypeAsync(entity.Id, agencyId!.Value, categoryId.Value, node, db); break;
            case "subpart": if (typeId.HasValue) subtypeId = await UpsertSubtypeAsync(typeId.Value, node, db); break;
            case "section":
                if (typeId.HasValue) sections.Add((node, typeId.Value, subtypeId, currentPath));
                return;
        }

        foreach (var child in node.Children)
            await UpsertHierarchyAsync(entity, child, db, sections, ct, agencyId, categoryId, typeId, subtypeId, currentPath);
    }

    private static async Task<Guid> UpsertAgencyAsync(Guid govEntityId, EcfrNode node, NpgsqlConnection db)
    {
        var existing = await db.QueryFirstOrDefaultAsync<Guid?>(
            @"SELECT ""Id"" FROM ""Agencies"" WHERE ""GovernmentEntityId""=@gid AND ""ChapterNumber""=@num AND ""IsDeleted""=false",
            new { gid = govEntityId, num = node.Identifier });
        if (existing.HasValue) return existing.Value;

        var id = Guid.NewGuid();
        await db.ExecuteAsync(
            @"INSERT INTO ""Agencies"" (""Id"",""GovernmentEntityId"",""ChapterNumber"",""AgencyName"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@gid,@num,@name,@now,@now,false)",
            new { id, gid = govEntityId, num = node.Identifier, name = node.Label, now = DateTime.UtcNow });
        return id;
    }

    private static async Task<Guid> UpsertCategoryAsync(Guid govEntityId, Guid agencyId, EcfrNode node, NpgsqlConnection db)
    {
        var existing = await db.QueryFirstOrDefaultAsync<Guid?>(
            @"SELECT ""Id"" FROM ""RegulationCategories"" WHERE ""AgencyId""=@aid AND ""SubchapterIdentifier""=@ident AND ""IsDeleted""=false",
            new { aid = agencyId, ident = node.Identifier });
        if (existing.HasValue) return existing.Value;

        var id = Guid.NewGuid();
        await db.ExecuteAsync(
            @"INSERT INTO ""RegulationCategories"" (""Id"",""GovernmentEntityId"",""AgencyId"",""SubchapterIdentifier"",""SubchapterName"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@gid,@aid,@ident,@name,@now,@now,false)",
            new { id, gid = govEntityId, aid = agencyId, ident = node.Identifier, name = node.Label, now = DateTime.UtcNow });
        return id;
    }

    private static async Task<Guid> UpsertTypeAsync(Guid govEntityId, Guid agencyId, Guid categoryId, EcfrNode node, NpgsqlConnection db)
    {
        int.TryParse(node.Identifier, out var partNumber);
        var existing = await db.QueryFirstOrDefaultAsync<Guid?>(
            @"SELECT ""Id"" FROM ""RegulationTypes"" WHERE ""RegulationCategoryId""=@cid AND ""PartNumber""=@num AND ""IsDeleted""=false",
            new { cid = categoryId, num = partNumber });
        if (existing.HasValue) return existing.Value;

        var id = Guid.NewGuid();
        await db.ExecuteAsync(
            @"INSERT INTO ""RegulationTypes"" (""Id"",""GovernmentEntityId"",""AgencyId"",""RegulationCategoryId"",""PartNumber"",""PartName"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@gid,@aid,@cid,@num,@name,@now,@now,false)",
            new { id, gid = govEntityId, aid = agencyId, cid = categoryId, num = partNumber, name = node.Label, now = DateTime.UtcNow });
        return id;
    }

    private static async Task<Guid> UpsertSubtypeAsync(Guid typeId, EcfrNode node, NpgsqlConnection db)
    {
        var existing = await db.QueryFirstOrDefaultAsync<Guid?>(
            @"SELECT ""Id"" FROM ""RegulationSubtypes"" WHERE ""RegulationTypeId""=@tid AND ""SubpartIdentifier""=@ident AND ""IsDeleted""=false",
            new { tid = typeId, ident = node.Identifier });
        if (existing.HasValue) return existing.Value;

        var id = Guid.NewGuid();
        await db.ExecuteAsync(
            @"INSERT INTO ""RegulationSubtypes"" (""Id"",""RegulationTypeId"",""SubpartIdentifier"",""SubpartName"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@tid,@ident,@name,@now,@now,false)",
            new { id, tid = typeId, ident = node.Identifier, name = node.Label, now = DateTime.UtcNow });
        return id;
    }

    private async Task InsertSectionBatchAsync(GovEntityRow entity, List<(EcfrNode Node, Guid TypeId, Guid? SubtypeId, string Path)> batch,
        DateOnly date, NpgsqlConnection db, CancellationToken ct)
    {
        foreach (var (node, typeId, subtypeId, sectionPath) in batch)
        {
            try
            {
                var html = await ecfrClient.GetSectionHtmlAsync(date, sectionPath, ct);
                var hash = HashContent(html);

                await db.ExecuteAsync(
                    @"INSERT INTO ""Regulations"" (""Id"",""GovernmentEntityId"",""RegulationTypeId"",""RegulationSubtypeId"",""SectionNumber"",""SectionName"",""HtmlContent"",""ContentHash"",""Version"",""IsActive"",""LastAmendedDate"",""CreatedAt"",""UpdatedAt"",""IsDeleted"") VALUES (@id,@gid,@tid,@stid,@snum,@sname,@html,@hash,1,true,@date,@now,@now,false) ON CONFLICT DO NOTHING",
                    new { id = Guid.NewGuid(), gid = entity.Id, tid = typeId, stid = subtypeId, snum = node.Identifier, sname = node.Label ?? "", html, hash, date, now = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to fetch section {Path}", sectionPath);
            }
        }
    }


    #region ?? Top-level orchestrator ???????????????????????????????????????????

    private async Task IncrementalSyncAsync(
        GovEntityRow entity,
        EcfrTitle title,
        NpgsqlConnection db,
        CancellationToken ct,
        NpgsqlConnection portalDb)
    {
        var since = entity.LastAmendedDateOnly ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var syncDate = title.LatestAmendedOn!.Value;

        logger.LogInformation(
            "[IncrementalSync] START – Title {TitleNumber} | Entity {EntityId} | Since {Since} | SyncDate {SyncDate}",
            entity.TitleNumber, entity.Id, since, syncDate);

        var versions = (await ecfrClient.GetVersionsSinceAsync(entity.TitleNumber, since, ct)).ToList();

        if (!versions.Any())
        {
            logger.LogInformation(
                "[IncrementalSync] No new versions found for Title {TitleNumber}. Skipping.",
                entity.TitleNumber);
        }

        var updated = 0;

        foreach (var version in versions)
        {
            logger.LogDebug(
                "[IncrementalSync] Processing version {Identifier} for Title {TitleNumber}",
                version.Identifier, entity.TitleNumber);

            try
            {
                // ?? Fetch remote HTML ?????????????????????????????????????????
                var html = await FetchSectionHtmlAsync(entity.TitleNumber, syncDate, version, ct);
                var newHash = HashContent(html);

                // ?? Load existing regulation ??????????????????????????????????
                var reg = await LoadRegulationAsync(db, entity.Id, version.Identifier);

                if (!ShouldUpdate(reg, newHash, version.Identifier))
                    continue;

                // ?? Open transactions ?????????????????????????????????????????
                await using var tx = await db.BeginTransactionAsync(ct);
                await using var portalTx = await portalDb.BeginTransactionAsync(ct);

                try
                {
                    var snapshot = CaptureSnapshot(reg!, html, newHash, syncDate);

                    // ?? Phase 1: Regulation processing ????????????????????????
                    await ExecuteRegulationPhaseAsync(reg!, snapshot, db, portalDb, tx, portalTx, ct);

                    // ?? Phase 2: Notification processing ?????????????????????
                    await ExecuteNotificationPhaseAsync(reg!, snapshot, portalDb, portalTx, ct);

                    // ?? Commit both databases ?????????????????????????????????
                    await tx.CommitAsync(ct);
                    await portalTx.CommitAsync(ct);

                    logger.LogInformation(
                        "[IncrementalSync] COMMITTED – RegulationId {RegId} | Section {Section} | Version {Ver} ? {NewVer}",
                        reg!.Id, reg.SectionNumber, reg.Version, snapshot.NewVersion);

                    updated++;
                }
                catch (Exception ex)
                {
                    await tx.RollbackAsync(ct);
                    await portalTx.RollbackAsync(ct);

                    logger.LogError(ex,
                        "[IncrementalSync] ROLLED BACK – RegulationId {RegId} | Section {Section} | Identifier {Identifier}",
                        reg!.Id, reg.SectionNumber, version.Identifier);

                    throw;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "[IncrementalSync] Failed to process version {Identifier} for Title {TitleNumber}",
                    version.Identifier, entity.TitleNumber);
            }
        }

        await UpdateEntityLastAmendedDateAsync(entity.Id, syncDate, db);

        logger.LogInformation(
            "[IncrementalSync] COMPLETE – Title {TitleNumber} | {Updated}/{Total} sections updated",
            entity.TitleNumber, updated, versions.Count);
    }

    #endregion

    // ?????????????????????????????????????????????????????????????????????????????
    #region ?? Phase 1: Regulation processing ????????????????????????????????????

    /// <summary>
    /// Updates the regulation row, archives change history, resolves the hierarchy,
    /// and creates the change notice record.
    /// </summary>
    private async Task ExecuteRegulationPhaseAsync(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        NpgsqlConnection db,
        NpgsqlConnection portalDb,
        NpgsqlTransaction tx,
        NpgsqlTransaction portalTx,
        CancellationToken ct)
    {
        logger.LogDebug(
            "[Phase1] START – RegulationId {RegId} | Section {Section}",
            reg.Id, reg.SectionNumber);

        await InsertChangeHistoryAsync(reg, snapshot, db, tx);
        await UpdateRegulationRowAsync(reg, snapshot, db, tx);

        snapshot.Hierarchy = await ResolveHierarchyAsync(reg, db, tx);

        await InsertChangeNoticeAsync(reg, snapshot, portalDb, portalTx);

        logger.LogInformation(
            "[Phase1] COMPLETE – RegulationId {RegId} | Section {Section} | NewVersion {Ver}",
            reg.Id, reg.SectionNumber, snapshot.NewVersion);
    }

    // ?? 1-A  Archive previous content ?????????????????????????????????????????

    private async Task InsertChangeHistoryAsync(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        NpgsqlConnection db,
        NpgsqlTransaction tx)
    {
        logger.LogDebug(
            "[InsertChangeHistory] Archiving RegulationId {RegId} | Version {Ver} | Hash {Hash}",
            reg.Id, reg.Version, reg.ContentHash);

        await db.ExecuteAsync(
            @"INSERT INTO ""RegulationChangeHistory""
          (""Id"",""RegulationId"",""SectionNumber"",""SectionName"",""HtmlContent"",
           ""ContentHash"",""Version"",""ChangeType"",""ArchivedAt"",""CreatedAt"",""UpdatedAt"",""IsDeleted"")
          VALUES
          (@id,@rid,@snum,@sname,@html,@hash,@ver,2,@now,@now,@now,false)",
            new
            {
                id = Guid.NewGuid(),
                rid = reg.Id,
                snum = reg.SectionNumber,
                sname = reg.SectionName,
                html = reg.HtmlContent,
                hash = reg.ContentHash,
                ver = reg.Version,
                now = DateTime.UtcNow
            },
            tx);

        logger.LogDebug(
            "[InsertChangeHistory] OK – RegulationId {RegId}", reg.Id);
    }

    // ?? 1-B  Apply update ?????????????????????????????????????????????????????

    private async Task UpdateRegulationRowAsync(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        NpgsqlConnection db,
        NpgsqlTransaction tx)
    {
        logger.LogDebug(
            "[UpdateRegulation] Applying update RegulationId {RegId} | Date {Date}",
            reg.Id, snapshot.NewAmendedDate);

        await db.ExecuteAsync(
            @"UPDATE ""Regulations""
          SET ""HtmlContent""=@html, ""ContentHash""=@hash,
              ""Version""=""Version""+1, ""LastAmendedDate""=@date, ""UpdatedAt""=@now
          WHERE ""Id""=@id",
            new
            {
                html = snapshot.NewHtml,
                hash = snapshot.NewHash,
                date = snapshot.NewAmendedDate,
                now = DateTime.UtcNow,
                id = reg.Id
            },
            tx);

        logger.LogDebug(
            "[UpdateRegulation] OK – RegulationId {RegId} | Hash {Hash}",
            reg.Id, snapshot.NewHash);
    }

    // ?? 1-C  Resolve hierarchy snapshot ??????????????????????????????????????

    private async Task<HierarchyDto> ResolveHierarchyAsync(
        RegulationRow reg,
        NpgsqlConnection db,
        NpgsqlTransaction tx)
    {
        logger.LogDebug(
            "[ResolveHierarchy] Resolving hierarchy for RegulationId {RegId} | EntityId {EntityId}",
            reg.Id, reg.GovernmentEntityId);

        var hierarchy = await db.QueryFirstOrDefaultAsync<HierarchyDto>(
            @"SELECT
            ge.""Id""             AS GovernmentEntityId,
            ge.""TitleName""      AS GovernmentEntityName,
            ag.""Id""             AS AgencyId,
            ag.""AgencyName""     AS AgencyName,
            rc.""Id""             AS RegulationCategoryId,
            rc.""SubchapterName"" AS CategoryName,
            rt.""Id""             AS RegulationTypeId,
            rt.""PartName""       AS TypeName,
            rs.""Id""             AS RegulationSubtypeId,
            rs.""SubpartName""    AS SubtypeName
          FROM  ""GovernmentEntities"" ge
          LEFT JOIN ""Agencies""             ag ON ag.""Id"" = @AgencyId   AND ag.""IsDeleted"" = false
          LEFT JOIN ""RegulationCategories"" rc ON rc.""Id"" = @CategoryId AND rc.""IsDeleted"" = false
          LEFT JOIN ""RegulationTypes""      rt ON rt.""Id"" = @TypeId     AND rt.""IsDeleted"" = false
          LEFT JOIN ""RegulationSubtypes""   rs ON rs.""Id"" = @SubtypeId  AND rs.""IsDeleted"" = false
          WHERE ge.""Id"" = @EntityId AND ge.""IsDeleted"" = false",
            new
            {
                EntityId = reg.GovernmentEntityId,
                AgencyId = reg.AgencyId,
                CategoryId = reg.RegulationCategoryId,
                TypeId = reg.RegulationTypeId,
                SubtypeId = reg.RegulationSubtypeId
            },
            tx)
            ?? throw new InvalidOperationException(
                $"Hierarchy not found for EntityId {reg.GovernmentEntityId}");

        logger.LogDebug(
            "[ResolveHierarchy] OK – Entity '{EntityName}' | Agency '{AgencyName}'",
            hierarchy.GovernmentEntityName, hierarchy.AgencyName);

        return hierarchy;
    }

    // ?? 1-D  Create change notice ?????????????????????????????????????????????

    private async Task InsertChangeNoticeAsync(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx)
    {
        var h = snapshot.Hierarchy!;

        logger.LogDebug(
            "[InsertChangeNotice] Creating notice for RegulationId {RegId} | Section {Section}",
            reg.Id, reg.SectionNumber);

        await portalDb.ExecuteAsync(
            @"INSERT INTO ""ChangeNotices""
          (""Id"",""RegulationId"",
           ""GovernmentEntityId"",""GovernmentEntityName"",
           ""AgencyId"",""AgencyName"",
           ""RegulationCategoryId"",""RegulationCategoryName"",
           ""RegulationTypeId"",""RegulationTypeName"",
           ""RegulationSubtypeId"",""RegulationSubtypeName"",
           ""SectionNumber"",""SectionTitle"",
           ""PreviousContentHash"",""NewContentHash"",
           ""PreviousHtmlContent"",""NewHtmlContent"",
           ""PreviousAmendedDate"",""NewAmendedDate"",
           ""ArchivedVersion"",""NewVersion"",
           ""ChangedAt"",""CreatedBy"")
          VALUES
          (@Id,@RegulationId,
           @GEId,@GEName,
           @AId,@AName,
           @CId,@CName,
           @TId,@TName,
           @STId,@STName,
           @SectionNumber,@SectionTitle,
           @PrevHash,@NewHash,
           @PrevHtml,@NewHtml,
           @PrevDate,@NewDate,
           @OldVer,@NewVer,
           @Now,'SYSTEM')",
            new
            {
                Id = Guid.NewGuid(),
                RegulationId = reg.Id,
                GEId = h.GovernmentEntityId,
                GEName = h.GovernmentEntityName,
                AId = h.AgencyId,
                AName = h.AgencyName,
                CId = h.RegulationCategoryId,
                CName = h.RegulationCategoryName,
                TId = h.RegulationTypeId,
                TName = h.RegulationTypeName,
                STId = h.RegulationSubtypeId,
                STName = h.RegulationSubtypeName,
                SectionNumber = reg.SectionNumber,
                SectionTitle = reg.SectionName,
                PrevHash = snapshot.PreviousHash,
                NewHash = snapshot.NewHash,
                PrevHtml = snapshot.PreviousHtml,
                NewHtml = snapshot.NewHtml,
                PrevDate = snapshot.PreviousAmendedDate,
                NewDate = snapshot.NewAmendedDate,
                OldVer = snapshot.PreviousVersion,
                NewVer = snapshot.NewVersion,
                Now = DateTime.UtcNow
            },
            portalTx);

        logger.LogDebug(
            "[InsertChangeNotice] OK – RegulationId {RegId}", reg.Id);
    }

    #endregion

    // ?????????????????????????????????????????????????????????????????????????????
    #region ?? Phase 2: Notification processing ?????????????????????????????????

    /// <summary>
    /// Matches subscriptions, resolves customers, builds email payloads,
    /// and inserts notification history.
    /// </summary>
    private async Task ExecuteNotificationPhaseAsync(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx,
        CancellationToken ct)
    {
        logger.LogDebug(
            "[Phase2] START – RegulationId {RegId}", reg.Id);

        var matchedSubs = await MatchSubscriptionsAsync(reg, portalDb, portalTx);

        if (!matchedSubs.Any())
        {
            logger.LogInformation(
                "[Phase2] No matching subscriptions for RegulationId {RegId}. Skipping notifications.",
                reg.Id);
            return;
        }

        var customers = await ResolveCustomersAsync(matchedSubs, portalDb, portalTx);
        var notifications = BuildNotifications(reg, snapshot, matchedSubs, customers);

        if (notifications.Any())
            await InsertNotificationHistoryAsync(notifications, portalDb, portalTx);

        logger.LogInformation(
            "[Phase2] COMPLETE – RegulationId {RegId} | Subscriptions {SubCount} | Notifications {NotifCount}",
            reg.Id, matchedSubs.Count, notifications.Count);
    }

    // ?? 2-A  Match subscriptions ??????????????????????????????????????????????

    private async Task<List<SubscriptionDto>> MatchSubscriptionsAsync(
        RegulationRow reg,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx)
    {
        logger.LogDebug(
            "[MatchSubscriptions] Querying subscriptions for RegulationId {RegId} | EntityId {EntityId}",
            reg.Id, reg.GovernmentEntityId);

        var subs = (await portalDb.QueryAsync<SubscriptionDto>(
            @"SELECT
            ""Id"",""CustomerId"",""GovernmentEntityId"",""AgencyId"",
            ""RegulationCategoryId"",""RegulationTypeId"",""RegulationSubtypeId"",
            ""RegulationId"",""SubscribingLevel""
          FROM ""Subscriptions""
          WHERE ""IsActive"" = true AND ""IsDeleted"" = false
            AND (
                (""SubscribingLevel"" = 0 AND ""GovernmentEntityId"" = @EntityId)
                OR
                (""SubscribingLevel"" = 1 AND ""GovernmentEntityId"" = @EntityId AND ""AgencyId"" = @AgencyId)
                OR
                (""SubscribingLevel"" = 2 AND ""GovernmentEntityId"" = @EntityId AND ""AgencyId"" = @AgencyId AND ""RegulationCategoryId"" = @CategoryId)
                OR
                (""SubscribingLevel"" = 3 AND ""GovernmentEntityId"" = @EntityId AND ""AgencyId"" = @AgencyId AND ""RegulationCategoryId"" = @CategoryId AND ""RegulationTypeId"" = @TypeId)
                OR
                (""SubscribingLevel"" = 4 AND ""GovernmentEntityId"" = @EntityId AND ""AgencyId"" = @AgencyId AND ""RegulationCategoryId"" = @CategoryId AND ""RegulationTypeId"" = @TypeId AND ""RegulationSubtypeId"" = @SubtypeId)
                OR
                (""SubscribingLevel"" = 5 AND ""GovernmentEntityId"" = @EntityId AND ""AgencyId"" = @AgencyId AND ""RegulationCategoryId"" = @CategoryId AND ""RegulationTypeId"" = @TypeId AND ""RegulationSubtypeId"" = @SubtypeId AND ""RegulationId"" = @RegulationId)
            )",
            new
            {
                EntityId = reg.GovernmentEntityId,
                AgencyId = reg.AgencyId,
                CategoryId = reg.RegulationCategoryId,
                TypeId = reg.RegulationTypeId,
                SubtypeId = reg.RegulationSubtypeId,
                RegulationId = reg.Id
            },
            portalTx))
            .ToList();

        logger.LogDebug(
            "[MatchSubscriptions] {Count} subscription(s) matched for RegulationId {RegId}",
            subs.Count, reg.Id);

        return subs;
    }

    // ?? 2-B  Resolve customers ????????????????????????????????????????????????

    private async Task<List<CustomerWithCompany>> ResolveCustomersAsync(
        List<SubscriptionDto> subs,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx)
    {
        var customerIds = subs.Select(s => s.CustomerId).Distinct().ToList();

        logger.LogDebug(
            "[ResolveCustomers] Resolving {Count} distinct customer(s)", customerIds.Count);

        var customers = (await portalDb.QueryAsync<CustomerWithCompany>(
            @"SELECT c.""Id"", c.""CompanyId"", c.""CustomerName"", c.""PrimaryEmail""
          FROM  ""Customers"" c
          INNER JOIN ""Companies"" co ON co.""Id"" = c.""CompanyId""
          WHERE c.""Id""  = ANY(@customerIds)
            AND c.""IsActive""  = true AND c.""IsDeleted""  = false
            AND co.""IsActive"" = true AND co.""IsDeleted"" = false",
            new { customerIds },
            portalTx))
            .ToList();

        var skipped = customerIds.Count - customers.Count;
        if (skipped > 0)
            logger.LogWarning(
                "[ResolveCustomers] {Skipped} customer(s) skipped (inactive / deleted)", skipped);

        logger.LogDebug(
            "[ResolveCustomers] Resolved {Count} active customer(s)", customers.Count);

        return customers;
    }

    // ?? 2-C  Build notification payloads ?????????????????????????????????????

    private List<NotificationHistoryDto> BuildNotifications(
        RegulationRow reg,
        ChangeSnapshot snapshot,
        List<SubscriptionDto> subs,
        List<CustomerWithCompany> customers)
    {
        logger.LogDebug(
            "[BuildNotifications] Building notifications for RegulationId {RegId}", reg.Id);

        var h = snapshot.Hierarchy!;
        var notifications = new List<NotificationHistoryDto>();

        foreach (var sub in subs)
        {
            var customer = customers.FirstOrDefault(c => c.Id == sub.CustomerId);
            if (customer is null)
            {
                logger.LogWarning(
                    "[BuildNotifications] Customer {CustomerId} not found – skipping subscription {SubId}",
                    sub.CustomerId, sub.Id);
                continue;
            }

            var (subject, body) = BuildRegulationChangeEmail(
                recipientName: customer.CustomerName,
                customerName: customer.CustomerName,
                governmentEntityName: h.GovernmentEntityName,
                agencyName: h.AgencyName,
                categoryName: h.RegulationCategoryName,
                typeName: h.RegulationTypeName,
                subtypeName: h.RegulationSubtypeName,
                sectionNumber: reg.SectionNumber,
                sectionTitle: reg.SectionName,
                changeType: "Updated",
                newVersion: snapshot.NewVersion,
                changedAt: DateTime.UtcNow,
                portalUrl: $"https://compliancehub.portal/regulations/{reg.Id}");

            notifications.Add(new NotificationHistoryDto
            {
                Id = Guid.NewGuid(),
                CompanyId = customer.CompanyId,
                CustomerId = customer.Id,
                SubscriptionId = sub.Id,
                GovernmentEntityId = h.GovernmentEntityId,
                GovernmentEntityName = h.GovernmentEntityName,
                AgencyId = h.AgencyId,
                AgencyName = h.AgencyName,
                RegulationCategoryId = h.RegulationCategoryId,
                RegulationCategoryName = h.RegulationCategoryName,
                RegulationTypeId = h.RegulationTypeId,
                RegulationTypeName = h.RegulationTypeName,
                RegulationSubtypeId = h.RegulationSubtypeId,
                RegulationSubtypeName = h.RegulationSubtypeName,
                PreviousRegulationId = reg.Id,
                PreviousRegulationName = reg.SectionName,
                PresentRegulationId = reg.Id,
                PresentRegulationName = reg.SectionName,
                Version = snapshot.NewVersion,
                Subject = subject,
                Body = body,
                SenderEmail = "system@compliancehub.com",
                SenderName = "Compliance Hub",
                RecipientEmail = customer.PrimaryEmail,
                RecipientName = customer.CustomerName,
                Status = "Pending",
                IsNotified = false,
                RetryCount = 3,
                CreatedAt = DateTime.UtcNow
            });
        }

        logger.LogDebug(
            "[BuildNotifications] {Count} notification(s) built for RegulationId {RegId}",
            notifications.Count, reg.Id);

        return notifications;
    }

    // ?? 2-D  Persist notifications ????????????????????????????????????????????

    private async Task InsertNotificationHistoryAsync(
        List<NotificationHistoryDto> notifications,
        NpgsqlConnection portalDb,
        NpgsqlTransaction portalTx)
    {
        logger.LogDebug(
            "[InsertNotificationHistory] Inserting {Count} notification(s)", notifications.Count);

        await portalDb.ExecuteAsync(
            @"INSERT INTO ""NotificationHistory""
          (""Id"",""CompanyId"",""CustomerId"",""SubscriptionId"",
           ""GovernmentEntityId"",""GovernmentEntityName"",
           ""AgencyId"",""AgencyName"",
           ""RegulationCategoryId"",""RegulationCategoryName"",
           ""RegulationTypeId"",""RegulationTypeName"",
           ""RegulationSubtypeId"",""RegulationSubtypeName"",
           ""PreviousRegulationId"",""PreviousRegulationName"",
           ""PresentRegulationId"",""PresentRegulationName"",
           ""Version"",""Subject"",""Body"",
           ""SenderEmail"",""SenderName"",
           ""RecipientEmail"",""RecipientName"",
           ""Status"",""IsNotified"",""RetryCount"",""CreatedAt"")
          VALUES
          (@Id,@CompanyId,@CustomerId,@SubscriptionId,
           @GovernmentEntityId,@GovernmentEntityName,
           @AgencyId,@AgencyName,
           @RegulationCategoryId,@RegulationCategoryName,
           @RegulationTypeId,@RegulationTypeName,
           @RegulationSubtypeId,@RegulationSubtypeName,
           @PreviousRegulationId,@PreviousRegulationName,
           @PresentRegulationId,@PresentRegulationName,
           @Version,@Subject,@Body,
           @SenderEmail,@SenderName,
           @RecipientEmail,@RecipientName,
           @Status,@IsNotified,@RetryCount,@CreatedAt)",
            notifications,
            portalTx);

        logger.LogDebug(
            "[InsertNotificationHistory] OK – {Count} record(s) inserted", notifications.Count);
    }

    #endregion

    // ?????????????????????????????????????????????????????????????????????????????
    #region ?? Utility / infrastructure helpers ?????????????????????????????????

    private async Task<string> FetchSectionHtmlAsync(
        int titleNumber, DateOnly date, EcfrVersionEntry ecfrVersionEntry, CancellationToken ct)
    {
        logger.LogDebug(
            "[FetchSectionHtml] Fetching HTML for {Identifier} | Date {Date}", ecfrVersionEntry.Identifier, date);

        var html = await ecfrClient.GetCurrentSectionHtmlAsync(date, ecfrVersionEntry, ct);

        logger.LogDebug(
            "[FetchSectionHtml] OK – {Identifier} | ContentLength {Length}", ecfrVersionEntry.Identifier, html.Length);

        return html;
    }

    private async Task<RegulationRow?> LoadRegulationAsync(
        NpgsqlConnection db, Guid entityId, string sectionNumber)
    {
        logger.LogDebug(
            "[LoadRegulation] EntityId {EntityId} | Section {Section}", entityId, sectionNumber);

        var reg = await db.QueryFirstOrDefaultAsync<RegulationRow>(
            @"SELECT
            ""Id"",""GovernmentEntityId"",""AgencyId"",""RegulationCategoryId"",
            ""RegulationTypeId"",""RegulationSubtypeId"",""SectionNumber"",""SectionName"",
            ""HtmlContent"",""ContentHash"",""Version"",""IsActive"",""LastAmendedDate""
          FROM ""Regulations""
          WHERE ""GovernmentEntityId"" = @gid
            AND ""SectionNumber"" = @snum
            AND ""IsDeleted"" = false
            AND ""IsActive""  = true",
            new { gid = entityId, snum = sectionNumber });

        if (reg is null)
            logger.LogWarning(
                "[LoadRegulation] No active regulation found for EntityId {EntityId} | Section {Section}",
                entityId, sectionNumber);

        return reg;
    }

    /// <summary>Returns <c>false</c> when the section can be safely skipped.</summary>
    private bool ShouldUpdate(RegulationRow? reg, string newHash, string identifier)
    {
        if (reg is null)
        {
            logger.LogDebug(
                "[ShouldUpdate] SKIP – regulation not found for {Identifier}", identifier);
            return false;
        }

        if (reg.ContentHash == newHash)
        {
            logger.LogDebug(
                "[ShouldUpdate] SKIP – hash unchanged for RegulationId {RegId}", reg.Id);
            return false;
        }

        logger.LogDebug(
            "[ShouldUpdate] CHANGED – RegulationId {RegId} | OldHash {Old} | NewHash {New}",
            reg.Id, reg.ContentHash, newHash);

        return true;
    }

    /// <summary>
    /// Captures an immutable snapshot of the before/after state so it can be
    /// threaded through both phases without repeated lookups.
    /// </summary>
    private static ChangeSnapshot CaptureSnapshot(
        RegulationRow reg, string newHtml, string newHash, DateOnly syncDate) =>
        new()
        {
            PreviousHtml = reg.HtmlContent,
            PreviousHash = reg.ContentHash,
            PreviousVersion = reg.Version,
            PreviousAmendedDate = reg.LastAmendedDate,
            NewHtml = newHtml,
            NewHash = newHash,
            NewVersion = reg.Version + 1,
            NewAmendedDate = syncDate
        };

    private async Task UpdateEntityLastAmendedDateAsync(
        Guid entityId, DateOnly date, NpgsqlConnection db)
    {
        logger.LogDebug(
            "[UpdateEntityDate] EntityId {EntityId} | Date {Date}", entityId, date);

        await db.ExecuteAsync(
            @"UPDATE ""GovernmentEntities""
          SET ""LastAmendedDate""=@date, ""UpdatedAt""=@now
          WHERE ""Id""=@id",
            new { date, now = DateTime.UtcNow, id = entityId });

        logger.LogDebug(
            "[UpdateEntityDate] OK – EntityId {EntityId}", entityId);
    }

    #endregion

    // ?????????????????????????????????????????????????????????????????????????????
    #region ?? Supporting data-transfer record ??????????????????????????????????

    /// <summary>
    /// Carries all before/after values for a single regulation change, plus the
    /// resolved hierarchy snapshot, across both processing phases.
    /// </summary>
    private sealed class ChangeSnapshot
    {
        // ?? Previous state ????????????????????????????????????????????????????
        public string? PreviousHtml { get; init; }
        public string? PreviousHash { get; init; }
        public int PreviousVersion { get; init; }
        public DateOnly? PreviousAmendedDate { get; init; }

        // ?? New state ?????????????????????????????????????????????????????????
        public string NewHtml { get; init; } = string.Empty;
        public string NewHash { get; init; } = string.Empty;
        public int NewVersion { get; init; }
        public DateOnly NewAmendedDate { get; init; }

        // ?? Resolved after Phase 1-C ?????????????????????????????????????????
        public HierarchyDto? Hierarchy { get; set; }
    }

    #endregion

    public (string Subject, string Body) BuildRegulationChangeEmail(
    string recipientName,
    string customerName,
    string governmentEntityName,
    string agencyName,
    string categoryName,
    string typeName,
    string? subtypeName,
    string sectionNumber,
    string sectionTitle,
    string changeType,
    int newVersion,
    DateTime changedAt,
    string portalUrl)
    {
        var subject = $"Compliance Hub Alert: Regulation {changeType} — Section {sectionNumber}";

        var body = $@"
<html>
<body style=""font-family:Arial,sans-serif;background:#f3f4f6;padding:24px;"">

  <div style=""max-width:720px;margin:0 auto;background:white;border-radius:10px;overflow:hidden;border:1px solid #e5e7eb;"">

    <!-- HEADER -->
    <div style=""background:#1e3a5f;padding:20px;"">
      <h2 style=""color:white;margin:0;font-size:18px;"">Regulation Update Notification</h2>
      <p style=""color:#93c5fd;margin:4px 0 0;font-size:13px;"">Compliance Hub</p>
    </div>

    <!-- BODY -->
    <div style=""padding:24px;"">

      <p style=""font-size:14px;"">
        Dear <strong>{recipientName}</strong>,
      </p>

      <p style=""font-size:14px;color:#374151;line-height:1.6;"">
        A regulation subscribed by <strong>{customerName}</strong> has been
        <strong style=""color:#1e3a5f;"">{changeType.ToLower()}</strong>.
      </p>

      <!-- HIERARCHY -->
      <div style=""margin-top:16px;border:1px solid #e5e7eb;border-radius:8px;padding:16px;"">

        <h3 style=""font-size:14px;margin-bottom:12px;"">Regulation Context</h3>

        <table style=""width:100%;font-size:13px;border-collapse:collapse;"">
          <tr><td style=""color:#6b7280;width:35%;"">Government Entity</td><td>{governmentEntityName}</td></tr>
          <tr><td style=""color:#6b7280;"">Agency</td><td>{agencyName}</td></tr>
          <tr><td style=""color:#6b7280;"">Category</td><td>{categoryName}</td></tr>
          <tr><td style=""color:#6b7280;"">Type</td><td>{typeName}</td></tr>
          <tr><td style=""color:#6b7280;"">Subtype</td><td>{(subtypeName ?? "N/A")}</td></tr>
        </table>

      </div>

      <!-- CHANGE INFO -->
      <div style=""margin-top:16px;border:1px solid #e5e7eb;border-radius:8px;padding:16px;background:#f9fafb;"">

        <h3 style=""font-size:14px;margin-bottom:12px;"">Latest Regulation Update</h3>

        <table style=""width:100%;font-size:13px;"">
          <tr><td style=""color:#6b7280;width:35%;"">Section</td><td>{sectionNumber}</td></tr>
          <tr><td style=""color:#6b7280;"">Title</td><td>{sectionTitle}</td></tr>
          <tr><td style=""color:#6b7280;"">Version</td><td>v{newVersion}</td></tr>
          <tr><td style=""color:#6b7280;"">Updated At</td><td>{changedAt:yyyy-MM-dd HH:mm:ss} UTC</td></tr>
        </table>

      </div>

      <!-- IMPORTANT CHANGE -->
      <div style=""margin-top:16px;padding:16px;border-left:4px solid #1e3a5f;background:#eff6ff;"">
        <p style=""margin:0;font-size:13px;color:#1e3a5f;"">
          This regulation has been updated in the federal eCFR repository.  
          The full structured regulation content is available in your Compliance Hub portal.
        </p>
      </div>

      <!-- CTA -->
      <div style=""margin-top:20px;text-align:center;"">
        <a href=""{portalUrl}""
           style=""background:#1e3a5f;color:white;padding:10px 16px;
                  text-decoration:none;border-radius:6px;font-size:13px;"">
          View Full Regulation
        </a>
      </div>

    </div>

    <!-- FOOTER -->
    <div style=""background:#f3f4f6;padding:12px;text-align:center;font-size:11px;color:#9ca3af;"">
      © Compliance Hub
    </div>

  </div>
</body>
</html>";

        return (subject, body);
    }

    private static string HashContent(string html) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(html))).ToLowerInvariant();
}
