using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class CompanyDivision : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
    public Department? Department { get; set; }
}
