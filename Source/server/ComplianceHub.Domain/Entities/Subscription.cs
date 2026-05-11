using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class Subscription : BaseEntity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public Guid GovernmentEntityId { get; set; }
    public Guid? AgencyId { get; set; }
    public Guid? RegulationCategoryId { get; set; }
    public Guid? RegulationTypeId { get; set; }
    public Guid? RegulationSubtypeId { get; set; }
    public Guid? RegulationId { get; set; }

    public SubscribingLevel SubscribingLevel { get; set; }
    public string SubscribedNodeName { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<NotificationHistory> NotificationHistories { get; set; } = [];
}
