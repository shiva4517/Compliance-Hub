using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class Employee : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
    public string? SecondaryEmail { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }

    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public Guid? CompanyDivisionId { get; set; }
    public CompanyDivision? CompanyDivision { get; set; }

    public Guid? CompanyDistrictId { get; set; }
    public District? District { get; set; }

    public string PrimaryAddress { get; set; } = string.Empty;
    public string PrimaryCity { get; set; } = string.Empty;
    public string PrimaryState { get; set; } = string.Empty;
    public string PrimaryPostalCode { get; set; } = string.Empty;

    public string? SecondaryAddress { get; set; }
    public string? SecondaryCity { get; set; }
    public string? SecondaryState { get; set; }
    public string? SecondaryPostalCode { get; set; }

    public bool IsActive { get; set; } = true;

    public string FullName => $"{FirstName} {LastName}";
}
