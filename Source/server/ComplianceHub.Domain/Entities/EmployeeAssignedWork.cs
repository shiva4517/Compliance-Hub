using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class EmployeeAssignedWork : BaseEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }

    public Guid SubscriptionId { get; set; }
    public Subscription? Subscription { get; set; }

    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignedBy { get; set; } = string.Empty;
}
