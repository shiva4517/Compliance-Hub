using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Entities.Regulations;
using ComplianceHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SyncLogLevel = ComplianceHub.Application.Common.Interfaces.SyncLogLevel;

namespace ComplianceHub.Infrastructure.Services.Sync;

public class RegulationSyncOrchestrator(
    IRegulationsUnitOfWork uow,
    IECFRApiClient ecfrClient,
    IOutboxProcessor outboxProcessor,
    ILogger<RegulationSyncOrchestrator> logger) : IRegulationSyncOrchestrator
{
    private const string InitialImportOperation = "InitialImport";
    private const string IncrementalSyncOperation = "IncrementalSync";
    private const string CompletedStatus = "Completed";
    private const string NoChangesDetectedStatus = "NoChangesDetected";
    private const string FailedStatus = "Failed";
    private const string SkippedStatus = "Skipped";

    public async Task RunAllEnabledAsync(string triggerSource = "Timer", CancellationToken ct = default)
    {
        var entities = await uow.GovernmentEntities.Query()
            .Where(e => e.IsSyncEnabled)
            .ToListAsync(ct);

        logger.LogInformation("Starting sync for {Count} government entities", entities.Count);

        foreach (var entity in entities)
        {
            try
            {
                await SyncGovernmentEntityAsync(entity.Id, null, triggerSource, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync failed for entity {Id} (Title {TitleNumber})", entity.Id, entity.TitleNumber);
            }
        }
    }

    public async Task<SyncResult> SyncGovernmentEntityAsync(Guid governmentEntityId, Func<string, SyncLogLevel, Task>? onProgress = null, string triggerSource = "Manual", CancellationToken ct = default)
    {
        var start = DateTime.UtcNow;
        GovernmentEntity? entity = null;
        var sectionsAdded = 0;
        var sectionsUpdated = 0;
        var sectionsDeactivated = 0;
        var outboxEventsQueued = 0;
        var sourceChangeCount = 0;
        string? operationType = null;
        var status = CompletedStatus;
        string? details = null;

        async Task Log(string message, SyncLogLevel level = SyncLogLevel.Info)
        {
            logger.LogInformation("{Message}", message);
            if (onProgress is not null)
            {
                await onProgress(message, level);
            }
        }

        try
        {
            entity = await uow.GovernmentEntities.Query()
                .FirstOrDefaultAsync(e => e.Id == governmentEntityId, ct)
                ?? throw new InvalidOperationException($"GovernmentEntity {governmentEntityId} not found");

            var titles = await ecfrClient.GetTitlesAsync(ct);
            var matchingTitle = titles.FirstOrDefault(t => t.Number == entity.TitleNumber);
            if (matchingTitle is null)
            {
                logger.LogWarning("Title {TitleNumber} not found in eCFR", entity.TitleNumber);

                operationType = entity.IsImported ? IncrementalSyncOperation : InitialImportOperation;
                status = SkippedStatus;
                details = $"Title {entity.TitleNumber} was not found in eCFR.";

                await TryRecordSyncHistoryAsync(
                    entity.Id,
                    operationType,
                    status,
                    start,
                    DateTime.UtcNow,
                    sectionsAdded,
                    sectionsUpdated,
                    sectionsDeactivated,
                    outboxEventsQueued,
                    details,
                    triggerSource,
                    ct);

                return new SyncResult(governmentEntityId, 0, 0, 0, 0, DateTime.UtcNow - start, "Title not found in eCFR");
            }

            var latestAmendedOn = matchingTitle.LatestAmendedOn ?? matchingTitle.LatestIssueDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

            if (!entity.IsImported)
            {
                operationType = InitialImportOperation;
                (sectionsAdded, outboxEventsQueued) = await RunInitialImportAsync(entity, latestAmendedOn, Log, ct);
                details = $"Initial import completed for Title {entity.TitleNumber}. Imported {sectionsAdded} record(s).";
            }
            else if (entity.LastAmendedDate is null || latestAmendedOn > entity.LastAmendedDate)
            {
                operationType = IncrementalSyncOperation;
                (sectionsUpdated, sectionsDeactivated, outboxEventsQueued, sourceChangeCount) =
                    await RunIncrementalSyncAsync(entity, latestAmendedOn, Log, ct);

                if (sectionsUpdated == 0 && sectionsDeactivated == 0)
                {
                    status = NoChangesDetectedStatus;
                    details = sourceChangeCount > 0
                        ? $"Incremental sync ran for Title {entity.TitleNumber}. eCFR reported {sourceChangeCount} potential change(s), but no material updates were applied."
                        : $"Incremental sync ran for Title {entity.TitleNumber}. No changes were reported by eCFR.";
                }
                else
                {
                    details = $"Incremental sync completed for Title {entity.TitleNumber}. Changed {sectionsUpdated} record(s) and deactivated {sectionsDeactivated} record(s).";
                }
            }
            else
            {
                operationType = IncrementalSyncOperation;
                status = NoChangesDetectedStatus;
                details = $"Incremental sync attempted for Title {entity.TitleNumber}. Data is already up to date.";
                await Log($"No changes detected for Title {entity.TitleNumber} - already up to date.");
            }

            entity.LastSyncedDate = DateTime.UtcNow;
            uow.GovernmentEntities.Update(entity);
            await uow.SaveChangesAsync(ct);

            await TryRecordSyncHistoryAsync(
                entity.Id,
                operationType ?? IncrementalSyncOperation,
                status,
                start,
                DateTime.UtcNow,
                sectionsAdded,
                sectionsUpdated,
                sectionsDeactivated,
                outboxEventsQueued,
                details,
                triggerSource,
                ct);

            return new SyncResult(governmentEntityId, sectionsAdded, sectionsUpdated, sectionsDeactivated, outboxEventsQueued, DateTime.UtcNow - start);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Sync error for entity {Id}", governmentEntityId);

            if (entity is not null)
            {
                await TryRecordSyncHistoryAsync(
                    entity.Id,
                    operationType ?? (entity.IsImported ? IncrementalSyncOperation : InitialImportOperation),
                    FailedStatus,
                    start,
                    DateTime.UtcNow,
                    sectionsAdded,
                    sectionsUpdated,
                    sectionsDeactivated,
                    outboxEventsQueued,
                    ex.Message,
                    triggerSource,
                    ct);
            }

            return new SyncResult(governmentEntityId, sectionsAdded, sectionsUpdated, sectionsDeactivated, outboxEventsQueued, DateTime.UtcNow - start, ex.Message);
        }
    }

    private async Task<(int sectionsAdded, int outboxEventsQueued)> RunInitialImportAsync(
        GovernmentEntity entity, DateOnly latestAmendedOn, Func<string, SyncLogLevel, Task> log, CancellationToken ct)
    {
        await log($"Running initial import for Title {entity.TitleNumber}...", SyncLogLevel.Info);

        var structure = await ecfrClient.GetTitleStructureAsync(entity.TitleNumber, latestAmendedOn, ct);
        var sectionNodes = new List<(EcfrStructureNodeDto node, Guid govId, Guid agencyId, Guid categoryId, Guid typeId, Guid? subtypeId, int partNumber)>();

        var hierarchyTracker = new HierarchyBatchTracker();
        await UpsertHierarchyAsync(entity, structure, sectionNodes, CancellationToken.None, hierarchyTracker, log: log);

        // Flush any remaining hierarchy inserts that didn't fill a full batch
        if (hierarchyTracker.PendingCount > 0)
        {
            try
            {
                await uow.SaveChangesAsync(CancellationToken.None);
                hierarchyTracker.TotalInserted += hierarchyTracker.PendingCount;
                logger.LogInformation(" Final flush saved {Pending} record(s). Grand total: {Total}", hierarchyTracker.PendingCount, hierarchyTracker.TotalInserted);
                await log($" Final flush — saved {hierarchyTracker.PendingCount} record(s). Total hierarchy records inserted: {hierarchyTracker.TotalInserted}.", SyncLogLevel.Success);
                hierarchyTracker.PendingCount = 0;
            }
            catch (OperationCanceledException)
            {
                logger.LogError("Hierarchy save operation was cancelled. This is typically due to command timeout with large datasets. Consider increasing CommandTimeout.");
                throw;
            }
        }
        else
        {
            await log($"Structure traversal complete. Total hierarchy records inserted: {hierarchyTracker.TotalInserted}.", SyncLogLevel.Info);
        }

        await log($"Discovered {sectionNodes.Count} section(s) to import.", SyncLogLevel.Info);

        var batchSize = 50;
        var sectionsAdded = 0;
        var sectionsSkipped = 0;
        var sectionsFailed = 0;
        var totalBatches = (sectionNodes.Count + batchSize - 1) / batchSize;

        for (var i = 0; i < sectionNodes.Count; i += batchSize)
        {
            var batch = sectionNodes.Skip(i).Take(batchSize).ToList();
            var batchNum = (i / batchSize) + 1;
            var rangeStart = i + 1;
            var rangeEnd = Math.Min(i + batchSize, sectionNodes.Count);

            await log($"[Batch {batchNum}/{totalBatches}] Processing sections {rangeStart}-{rangeEnd} of {sectionNodes.Count}...", SyncLogLevel.Info);
            logger.LogInformation("[Batch {BatchNum}/{TotalBatches}] Starting batch processing of {BatchSize} section(s)", batchNum, totalBatches, batch.Count);

            var batchAdded = 0;
            var batchSkipped = 0;
            var batchFailed = 0;

            foreach (var (node, govId, agencyId, categoryId, typeId, subtypeId, partNumber) in batch)
            {
                try
                {
                    logger.LogDebug("Fetching HTML for section {SectionNumber} (Part {PartNumber}, Title {TitleNumber})", node.Identifier, partNumber, entity.TitleNumber);
                    var html = await FetchSectionContentWithRetryAsync(entity.TitleNumber, partNumber, node.Identifier);

                    if (string.IsNullOrEmpty(html))
                    {
                        logger.LogWarning("[Batch {BatchNum}] Skipping section {SectionNumber} — no HTML content returned", batchNum, node.Identifier);
                        await log($"[Batch {batchNum}] Skipping section {node.Identifier} — no HTML content returned.", SyncLogLevel.Warning);
                        batchSkipped++;
                        sectionsSkipped++;
                        continue;
                    }

                    var hash = ComputeHash(html);
                    var regulation = new Regulation
                    {
                        Identifier = node.Identifier,
                        SectionNumber = node.Identifier,
                        SectionName = node.Label,
                        HtmlContent = html,
                        ContentHash = hash,
                        Version = 1,
                        IsActive = true,
                        LastAmendedDate = latestAmendedOn,
                        GovernmentEntityId = govId,
                        AgencyId = agencyId,
                        RegulationCategoryId = categoryId,
                        RegulationTypeId = typeId,
                        RegulationSubtypeId = subtypeId
                    };

                    await uow.Regulations.AddAsync(regulation);
                    batchAdded++;
                    sectionsAdded++;
                    logger.LogDebug("[Batch {BatchNum}] Queued section {SectionNumber} for insert", batchNum, node.Identifier);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[Batch {BatchNum}] Failed to fetch or process section {SectionNumber} after retries", batchNum, node.Identifier);
                    await log($"[Batch {batchNum}] ERROR — Failed to process section {node.Identifier}: {ex.Message}", SyncLogLevel.Error);
                    batchFailed++;
                    sectionsFailed++;
                }
            }

            await log($"[Batch {batchNum}/{totalBatches}] Saving {batchAdded} section(s) to database (skipped: {batchSkipped}, failed: {batchFailed})...", SyncLogLevel.Info);
            logger.LogInformation("[Batch {BatchNum}/{TotalBatches}] Persisting batch — added: {Added}, skipped: {Skipped}, failed: {Failed}", batchNum, totalBatches, batchAdded, batchSkipped, batchFailed);

            await uow.SaveChangesAsync(CancellationToken.None);

            await log($"[Batch {batchNum}/{totalBatches}] Batch saved. Running total: {sectionsAdded} added, {sectionsSkipped} skipped, {sectionsFailed} failed.", SyncLogLevel.Success);
            logger.LogInformation("[Batch {BatchNum}/{TotalBatches}] Batch saved. Cumulative — added: {TotalAdded}, skipped: {TotalSkipped}, failed: {TotalFailed}", batchNum, totalBatches, sectionsAdded, sectionsSkipped, sectionsFailed);

            await Task.Delay(1000, CancellationToken.None);
        }

        entity.IsImported = true;
        entity.LastAmendedDate = latestAmendedOn;
        uow.GovernmentEntities.Update(entity);
        await uow.SaveChangesAsync(CancellationToken.None);

        await log($"Initial import complete — {sectionsAdded} section(s) added, {sectionsSkipped} skipped, {sectionsFailed} failed.", SyncLogLevel.Success);
        logger.LogInformation("Initial import finished for Title {TitleNumber}. Added: {Added}, Skipped: {Skipped}, Failed: {Failed}", entity.TitleNumber, sectionsAdded, sectionsSkipped, sectionsFailed);
        return (sectionsAdded, 0);
    }

    private async Task<string> FetchSectionContentWithRetryAsync(int titleNumber, int partNumber, string sectionIdentifier, int maxRetries = 3)
    {
        const int initialDelayMs = 500;
        var delayMs = initialDelayMs;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                return await ecfrClient.GetCurrentSectionContentHtmlAsync(titleNumber, partNumber, sectionIdentifier, CancellationToken.None);
            }
            catch (HttpRequestException ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "HTTP request failed for section {SectionNumber} (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms",
                    sectionIdentifier, attempt, maxRetries, delayMs);
                await Task.Delay(delayMs, CancellationToken.None);
                delayMs = (int)(delayMs * 1.5);
            }
            catch (OperationCanceledException ex) when (attempt < maxRetries)
            {
                logger.LogWarning(ex, "Request timeout for section {SectionNumber} (attempt {Attempt}/{MaxRetries}), retrying in {DelayMs}ms",
                    sectionIdentifier, attempt, maxRetries, delayMs);
                await Task.Delay(delayMs, CancellationToken.None);
                delayMs = (int)(delayMs * 1.5);
            }
        }

        return await ecfrClient.GetCurrentSectionContentHtmlAsync(titleNumber, partNumber, sectionIdentifier, CancellationToken.None);
    }

    private async Task<(int sectionsUpdated, int sectionsDeactivated, int outboxEventsQueued, int sourceChangeCount)> RunIncrementalSyncAsync(
        GovernmentEntity entity, DateOnly latestAmendedOn, Func<string, SyncLogLevel, Task> log, CancellationToken ct)
    {
        await log($"Running incremental sync for Title {entity.TitleNumber} (changes since {entity.LastAmendedDate})...", SyncLogLevel.Info);

        var changes = await ecfrClient.GetVersionsSinceAsync(entity.TitleNumber, entity.LastAmendedDate!.Value, ct);
        var sectionsUpdated = 0;
        var sectionsDeactivated = 0;
        var outboxEventsQueued = 0;
        //var sourceChangeCount =  changes?.Meta?.ResultCount ?? 0;
        var sourceChangeCount = int.TryParse(changes?.Meta?.ResultCount, out var count)
    ? count
    : 0;

        if (changes != null)
        {
            await log($"Found {changes.Meta.ResultCount} change(s) as of {changes.Meta.LatestAmendmentDate} for govt. entity {changes.Meta.Title}...", SyncLogLevel.Info);

            foreach (var change in changes.ContentVersions)
            {
                var html = await ecfrClient.GetCurrentSectionContentHtmlAsync(
                    Convert.ToInt32(change.Title),
                    Convert.ToInt32(change.Part),
                    change.Identifier,
                    ct);

                var regulation = await uow.Regulations.Query()
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.GovernmentEntityId == entity.Id && r.Identifier == change.Identifier, ct);

                if (regulation is null)
                {
                    continue;
                }

                var newHash = ComputeHash(html);
                if (newHash == regulation.ContentHash)
                {
                    continue;
                }

                var changeType = string.IsNullOrEmpty(html) ? RegulationChangeType.Deactivate : RegulationChangeType.Update;
                await TransactionProcessAsync(regulation, html, newHash, changeType, latestAmendedOn, ct);

                if (changeType == RegulationChangeType.Deactivate)
                {
                    sectionsDeactivated++;
                }
                else
                {
                    sectionsUpdated++;
                }

                outboxEventsQueued++;
            }

            entity.LastAmendedDate = latestAmendedOn;
            uow.GovernmentEntities.Update(entity);
            await uow.SaveChangesAsync(ct);
        }

        return (sectionsUpdated, sectionsDeactivated, outboxEventsQueued, sourceChangeCount);
    }

    private async Task TryRecordSyncHistoryAsync(
        Guid governmentEntityId,
        string operationType,
        string status,
        DateTime startedAt,
        DateTime completedAt,
        int importedRecordsCount,
        int changedRecordsCount,
        int deactivatedRecordsCount,
        int outboxEventsQueuedCount,
        string? details,
        string triggerSource,
        CancellationToken ct)
    {
        try
        {
            await uow.RegulationSyncSchedulerHistories.AddAsync(new RegulationSyncSchedulerHistory
            {
                GovernmentEntityId = governmentEntityId,
                OperationType = operationType,
                Status = status,
                ImportedRecordsCount = importedRecordsCount,
                ChangedRecordsCount = changedRecordsCount,
                DeactivatedRecordsCount = deactivatedRecordsCount,
                OutboxEventsQueuedCount = outboxEventsQueuedCount,
                Details = details,
                StartedAt = startedAt,
                CompletedAt = completedAt,
                TriggerSource = triggerSource
            });

            await uow.SaveChangesAsync(ct);
        }
        catch (Exception historyEx)
        {
            logger.LogError(historyEx, "Failed to record sync history for government entity {GovernmentEntityId}", governmentEntityId);
        }
    }

    private async Task TransactionProcessAsync(
        Regulation regulation, string newHtml, string newHash,
        RegulationChangeType changeType, DateOnly newDate, CancellationToken ct)
    {
        await uow.ExecuteInTransactionAsync(async innerCt =>
        {
            await uow.RegulationChangeHistory.AddAsync(new RegulationChangeHistory
            {
                RegulationId = regulation.Id,
                SectionNumber = regulation.SectionNumber,
                HtmlContent = regulation.HtmlContent,
                ContentHash = regulation.ContentHash,
                Version = regulation.Version,
                ChangeType = changeType,
                ArchivedAt = DateTime.UtcNow
            });

            regulation.HtmlContent = newHtml;
            regulation.ContentHash = newHash;
            regulation.Version++;
            regulation.LastAmendedDate = newDate;
            if (changeType == RegulationChangeType.Deactivate)
            {
                regulation.IsActive = false;
            }

            uow.Regulations.Update(regulation);

            await uow.OutboxEvents.AddAsync(new OutboxEvent
            {
                EventType = "RegulationChanged",
                Payload = JsonSerializer.Serialize(new
                {
                    RegulationId = regulation.Id,
                    GovernmentEntityId = regulation.GovernmentEntityId,
                    regulation.SectionNumber,
                    ChangeType = changeType.ToString(),
                    regulation.Version
                })
            });

            await uow.SaveChangesAsync(innerCt);
        }, ct);

        await outboxProcessor.ProcessPendingAsync(ct);
    }

    private async Task UpsertHierarchyAsync(
        GovernmentEntity entity,
        EcfrStructureNodeDto node,
        List<(EcfrStructureNodeDto, Guid, Guid, Guid, Guid, Guid?, int)> sectionNodes,
        CancellationToken ct,
        HierarchyBatchTracker tracker,
        Agency? currentAgency = null,
        RegulationCategory? currentCategory = null,
        RegulationType? currentType = null,
        RegulationSubtype? currentSubtype = null,
        Func<string, SyncLogLevel, Task>? log = null)
    {
        const int hierarchyBatchSize = 100;

        try
        {
            switch (node.Type.ToLower())
            {
                case "chapter":
                    {
                        var agency = await uow.Agencies.Query()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(a => a.GovernmentEntityId == entity.Id && a.ChapterNumber == node.Identifier, ct);

                        if (agency is null)
                        {
                            agency = new Agency
                            {
                                Identifier = node.Identifier,
                                GovernmentEntityId = entity.Id,
                                ChapterNumber = node.Identifier,
                                AgencyName = node.Label
                            };
                            await uow.Agencies.AddAsync(agency);
                            tracker.PendingCount++;
                            logger.LogInformation("Queued Agency — Chapter: {Identifier}, Name: {Label} (pending: {Pending})", node.Identifier, node.Label, tracker.PendingCount);
                            if (log is not null) await log($"New agency queued: Chapter {node.Identifier} — {node.Label}", SyncLogLevel.Info);
                            await FlushHierarchyBatchIfNeededAsync(tracker, hierarchyBatchSize, ct, log);
                        }
                        else
                        {
                            if (log is not null) await log($"Agency already exists — Chapter: {agency.AgencyName}", SyncLogLevel.Warning);
                        }

                        currentAgency = agency;
                        break;
                    }
                case "subchapter":
                    {
                        if (currentAgency is null)
                        {
                            logger.LogWarning("Skipping subchapter {Identifier} — no parent agency in scope", node.Identifier);
                            if (log is not null) await log($"Skipping subchapter {node.Identifier} — no parent agency in scope.", SyncLogLevel.Warning);
                            break;
                        }

                        var category = await uow.RegulationCategories.Query()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(c => c.AgencyId == currentAgency.Id && c.SubchapterIdentifier == node.Identifier, ct);

                        if (category is null)
                        {
                            category = new RegulationCategory
                            {
                                Identifier = node.Identifier,
                                GovernmentEntityId = entity.Id,
                                AgencyId = currentAgency.Id,
                                SubchapterIdentifier = node.Identifier,
                                SubchapterName = node.Label
                            };
                            await uow.RegulationCategories.AddAsync(category);
                            tracker.PendingCount++;
                            logger.LogInformation("Queued RegulationCategory — Subchapter: {Identifier}, Name: {Label}, AgencyId: {AgencyId} (pending: {Pending})", node.Identifier, node.Label, currentAgency.Id, tracker.PendingCount);
                            if (log is not null) await log($"New category queued: Subchapter {node.Identifier} — {node.Label}", SyncLogLevel.Info);
                            await FlushHierarchyBatchIfNeededAsync(tracker, hierarchyBatchSize, ct, log);
                        }
                        else
                        {
                            if (log is not null) await log($"RegulationCategory already exists — Subchapter: {category.SubchapterName}", SyncLogLevel.Warning);
                        }

                        currentCategory = category;
                        break;
                    }
                case "part":
                    {
                        if (currentAgency is null || !int.TryParse(node.Identifier, out var partNumber))
                        {
                            logger.LogWarning("Skipping part {Identifier} — no parent agency or invalid part number", node.Identifier);
                            if (log is not null) await log($"Skipping part {node.Identifier} — no parent agency or invalid part number.", SyncLogLevel.Warning);
                            break;
                        }

                        var categoryId = currentCategory?.Id;
                        if (categoryId is null)
                        {
                            logger.LogDebug("Part {Identifier} has no subchapter — resolving or creating default category for Agency {AgencyId}", node.Identifier, currentAgency.Id);

                            var defaultCategory = await uow.RegulationCategories.Query()
                                .IgnoreQueryFilters()
                                .FirstOrDefaultAsync(c => c.AgencyId == currentAgency.Id, ct);

                            if (defaultCategory is null)
                            {
                                defaultCategory = new RegulationCategory
                                {
                                    Identifier = "DEFAULT",
                                    GovernmentEntityId = entity.Id,
                                    AgencyId = currentAgency.Id,
                                    SubchapterIdentifier = "DEFAULT",
                                    SubchapterName = "Default"
                                };
                                await uow.RegulationCategories.AddAsync(defaultCategory);
                                tracker.PendingCount++;
                                await uow.SaveChangesAsync(ct);
                                tracker.TotalInserted += tracker.PendingCount;
                                tracker.PendingCount = 0;
                                logger.LogInformation("Inserted default RegulationCategory for Agency {AgencyId}. Total inserted so far: {Total}", currentAgency.Id, tracker.TotalInserted);
                                if (log is not null) await log($"Created and saved default category for agency {currentAgency.Id}. Total inserted: {tracker.TotalInserted}.", SyncLogLevel.Info);
                            }

                            categoryId = defaultCategory.Id;
                        }

                        var type = await uow.RegulationTypes.Query()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(t => t.RegulationCategoryId == categoryId && t.PartNumber == partNumber, ct);

                        if (type is null)
                        {
                            type = new RegulationType
                            {
                                Identifier = node.Identifier,
                                GovernmentEntityId = entity.Id,
                                AgencyId = currentAgency.Id,
                                RegulationCategoryId = categoryId.Value,
                                PartNumber = partNumber,
                                PartName = node.Label
                            };
                            await uow.RegulationTypes.AddAsync(type);
                            tracker.PendingCount++;
                            logger.LogInformation("Queued RegulationType — Part: {PartNumber}, Name: {Label}, CategoryId: {CategoryId} (pending: {Pending})", partNumber, node.Label, categoryId, tracker.PendingCount);
                            if (log is not null) await log($"New regulation type queued: Part {partNumber} — {node.Label}", SyncLogLevel.Info);
                            await FlushHierarchyBatchIfNeededAsync(tracker, hierarchyBatchSize, ct, log);
                        }
                        else
                        {
                            if (log is not null) await log($"RegulationType already exists — Part: {type.PartName}", SyncLogLevel.Warning);
                        }

                        currentType = type;
                        break;
                    }
                case "subpart":
                    {
                        if (currentType is null)
                        {
                            logger.LogWarning("Skipping subpart {Identifier} — no parent type in scope", node.Identifier);
                            if (log is not null) await log($"Skipping subpart {node.Identifier} — no parent type in scope.", SyncLogLevel.Warning);
                            break;
                        }

                        var subtype = await uow.RegulationSubtypes.Query()
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(s => s.RegulationTypeId == currentType.Id && s.SubpartIdentifier == node.Identifier, ct);

                        if (subtype is null)
                        {
                            subtype = new RegulationSubtype
                            {
                                Identifier = node.Identifier,
                                RegulationTypeId = currentType.Id,
                                SubpartIdentifier = node.Identifier,
                                SubpartName = node.Label
                            };
                            await uow.RegulationSubtypes.AddAsync(subtype);
                            tracker.PendingCount++;
                            logger.LogInformation("Queued RegulationSubtype — Subpart: {Identifier}, Name: {Label}, TypeId: {TypeId} (pending: {Pending})", node.Identifier, node.Label, currentType.Id, tracker.PendingCount);
                            if (log is not null) await log($"New subtype queued: Subpart {node.Identifier} — {node.Label}", SyncLogLevel.Info);
                            await FlushHierarchyBatchIfNeededAsync(tracker, hierarchyBatchSize, ct, log);
                        }
                        else
                        {
                            if (log is not null) await log($"RegulationSubtype already exists — Subpart: {subtype.SubpartName}", SyncLogLevel.Warning);
                        }

                        currentSubtype = subtype;
                        break;
                    }
                case "section":
                    {
                        if (currentType is null)
                        {
                            logger.LogWarning("Skipping section {Identifier} — no parent type in scope", node.Identifier);
                            if (log is not null) await log($"Skipping section {node.Identifier} — no parent type in scope.", SyncLogLevel.Warning);
                            break;
                        }

                        sectionNodes.Add((node, entity.Id, currentAgency!.Id, currentType.RegulationCategoryId, currentType.Id, currentSubtype?.Id, currentType.PartNumber));
                        if (log is not null) await log($"\"{node.Label} — queued for content fetch (Part {currentType.PartNumber}) ", SyncLogLevel.Warning);
                        break;
                    }
                default:
                    logger.LogDebug("Ignoring unrecognized node type '{NodeType}' with identifier {Identifier}", node.Type, node.Identifier);
                    break;
            }

            foreach (var child in node.Children)
            {
                await UpsertHierarchyAsync(entity, child, sectionNodes, ct, tracker, currentAgency, currentCategory, currentType, currentSubtype, log);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing {NodeType} node with identifier {NodeIdentifier}", node.Type, node.Identifier);
            throw;
        }
    }

    private async Task FlushHierarchyBatchIfNeededAsync(HierarchyBatchTracker tracker, int batchSize, CancellationToken ct, Func<string, SyncLogLevel, Task>? log)
    {
        if (tracker.PendingCount < batchSize) return;

        await uow.SaveChangesAsync(ct);
        tracker.TotalInserted += tracker.PendingCount;
        var message = $"Batch saved — {tracker.PendingCount} record(s) flushed. Total hierarchy records inserted so far: {tracker.TotalInserted}.";
        tracker.PendingCount = 0;

        logger.LogInformation("{Message}", message);
        if (log is not null) await log(message, SyncLogLevel.Success);
    }

    private sealed class HierarchyBatchTracker
    {
        public int PendingCount { get; set; }
        public int TotalInserted { get; set; }
    }

    private static string BuildSectionPath(int titleNumber, string sectionIdentifier)
        => $"title-{titleNumber}/section-{sectionIdentifier}";

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..64];
    }
}
