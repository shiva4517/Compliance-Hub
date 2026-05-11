namespace ComplianceHub.Domain.Entities;

public class NotificationSentHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationHistoryId { get; set; }
    public int AttemptNumber { get; set; }
    public bool IsSuccess { get; set; }

    /// <summary>Sent | Failed | DeadLettered</summary>
    public string Status { get; set; } = string.Empty;

    public string? FailureReason { get; set; }
    public DateTime AttemptedAt { get; set; } = DateTime.UtcNow;
}
