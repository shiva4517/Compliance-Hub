using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class Grievance : BaseEntity
{
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }
    public Company? Company { get; set; }

    public Guid? SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public Guid CreatedBySecurityUserId { get; set; }
    public SecurityUser? CreatedBySecurityUser { get; set; }
    public UserRole CreatedByUserType { get; set; }

    public Guid RecipientSecurityUserId { get; set; }
    public SecurityUser? RecipientSecurityUser { get; set; }
    public UserRole RecipientUserType { get; set; }

    public GrievanceStatus Status { get; set; } = GrievanceStatus.Open;
    public GrievancePriority Priority { get; set; } = GrievancePriority.Medium;
    public bool IsActive { get; set; } = true;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    public ICollection<GrievanceReply> Replies { get; set; } = [];
}
