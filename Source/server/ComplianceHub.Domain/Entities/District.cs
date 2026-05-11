using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities;

public class District : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Company Company { get; set; } = null!;
}
