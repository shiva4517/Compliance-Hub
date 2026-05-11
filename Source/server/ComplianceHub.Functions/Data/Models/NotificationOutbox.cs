namespace ComplianceHub.Functions.Data.Models;

public class NotificationOutbox
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid NotificationHistoryId { get; set; }

    /// <summary>Pending → Processing → Published | Failed</summary>
    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public string? FailureReason { get; set; }
}
