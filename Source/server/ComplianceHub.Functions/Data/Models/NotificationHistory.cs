namespace ComplianceHub.Functions.Data.Models;

public class NotificationHistory
{
    public Guid Id { get; set; }
    public Guid SubscriptionId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? CompanyId { get; set; }

    public Guid? GovernmentEntityId { get; set; }
    public string? GovernmentEntityName { get; set; }
    public Guid? AgencyId { get; set; }
    public string? AgencyName { get; set; }
    public Guid? RegulationCategoryId { get; set; }
    public string? RegulationCategoryName { get; set; }
    public Guid? RegulationTypeId { get; set; }
    public string? RegulationTypeName { get; set; }
    public Guid? RegulationSubtypeId { get; set; }
    public string? RegulationSubtypeName { get; set; }

    public Guid? PreviousRegulationId { get; set; }
    public string? PreviousRegulationName { get; set; }
    public Guid? PresentRegulationId { get; set; }
    public string? PresentRegulationName { get; set; }

    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? SenderEmail { get; set; }
    public string? SenderName { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }

    public bool IsNotified { get; set; }
    public string Status { get; set; } = "Pending";
    public int RetryCount { get; set; }
    public DateTime? LastRetryAt { get; set; }
    public string? FailureReason { get; set; }

    public DateTime CreatedAt { get; set; }
}
