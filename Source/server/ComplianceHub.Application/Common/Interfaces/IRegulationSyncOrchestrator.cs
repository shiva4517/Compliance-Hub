namespace ComplianceHub.Application.Common.Interfaces;

public enum SyncLogLevel
{
    Info,
    Success,
    Warning,
    Error
}

public record SyncResult(
    Guid GovernmentEntityId,
    int SectionsAdded,
    int SectionsUpdated,
    int SectionsDeactivated,
    int OutboxEventsQueued,
    TimeSpan Duration,
    string? Error = null);

public interface IRegulationSyncOrchestrator
{
    Task<SyncResult> SyncGovernmentEntityAsync(Guid governmentEntityId, Func<string, SyncLogLevel, Task>? onProgress = null, string triggerSource = "Manual", CancellationToken ct = default);
    Task RunAllEnabledAsync(string triggerSource = "Timer", CancellationToken ct = default);
}
