using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationSyncSchedulerHistory : BaseEntity
{
    public Guid GovernmentEntityId { get; set; }
    public GovernmentEntity? GovernmentEntity { get; set; }

    public string OperationType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string TriggerSource { get; set; } = "Manual";
    public int ImportedRecordsCount { get; set; }
    public int ChangedRecordsCount { get; set; }
    public int DeactivatedRecordsCount { get; set; }
    public int OutboxEventsQueuedCount { get; set; }
    public string? Details { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
}
