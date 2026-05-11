using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class Company : BaseEntity
{
    public string CompanyCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string? SecondaryEmail { get; set; }
    public string? PhoneNumber { get; set; }

    // Primary Address
    public string PrimaryAddress { get; set; } = string.Empty;
    public string PrimaryCity { get; set; } = string.Empty;
    public string PrimaryState { get; set; } = string.Empty;
    public string PrimaryPostalCode { get; set; } = string.Empty;

    // Secondary Address
    public string? SecondaryAddress { get; set; }
    public string? SecondaryCity { get; set; }
    public string? SecondaryState { get; set; }
    public string? SecondaryPostalCode { get; set; }

    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CompanyDivision> Divisions { get; set; } = [];
}
