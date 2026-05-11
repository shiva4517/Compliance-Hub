using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class Customer : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string PrimaryContactFirstName { get; set; } = string.Empty;
    public string PrimaryContactLastName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string? SecondaryEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }

    public string? PrimaryAddress { get; set; }
    public string? PrimaryCity { get; set; }
    public string? PrimaryState { get; set; }
    public string? PrimaryPostalCode { get; set; }

    public string? SecondaryAddress { get; set; }
    public string? SecondaryCity { get; set; }
    public string? SecondaryState { get; set; }
    public string? SecondaryPostalCode { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Subscription> Subscriptions { get; set; } = [];
    public ICollection<NotificationHistory> NotificationHistories { get; set; } = [];

    public string FullContactName => $"{PrimaryContactFirstName} {PrimaryContactLastName}";
}
