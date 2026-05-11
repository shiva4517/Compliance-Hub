using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class SecurityGroup : BaseEntity
{
    public string GroupName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<SecurityUser> Users { get; set; } = new List<SecurityUser>();
}
