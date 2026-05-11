using System.Collections.Concurrent;

namespace ComplianceHub.API.Services;

public enum SyncJobStatus { Running, Done, Error }

public record SyncLogEntry(string Message, string Level);

public record SyncResultSnapshot(
    int SectionsAdded,
    int SectionsUpdated,
    int SectionsDeactivated,
    int OutboxEventsQueued,
    string Duration,
    string? Error);

public class SyncJob
{
    private readonly List<SyncLogEntry> _logs = [];
    private readonly object _lock = new();

    public SyncJobStatus Status { get; set; } = SyncJobStatus.Running;
    public string? Error { get; set; }
    public SyncResultSnapshot? Summary { get; set; }

    public void AddLog(string message, string level)
    {
        lock (_lock) _logs.Add(new SyncLogEntry(message, level));
    }

    public IReadOnlyList<SyncLogEntry> GetLogs()
    {
        lock (_lock) return [.. _logs];
    }
}

public class SyncJobStore
{
    private readonly ConcurrentDictionary<Guid, SyncJob> _jobs = new();

    public (Guid JobId, SyncJob Job) Create()
    {
        var id = Guid.NewGuid();
        var job = new SyncJob();
        _jobs[id] = job;
        return (id, job);
    }

    public SyncJob? Get(Guid jobId) => _jobs.GetValueOrDefault(jobId);
}
