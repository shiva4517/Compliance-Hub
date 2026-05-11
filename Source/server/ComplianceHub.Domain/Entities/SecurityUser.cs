using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class SecurityUser : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Title { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }

    public Guid? SecurityGroupId { get; set; }
    public SecurityGroup? SecurityGroup { get; set; }

    /// <summary>
    /// Polymorphic reference: maps to Customers.Id (Role=Customer), Employees.Id (Role=Employee), or Companies.Id (Role=Admin).
    /// </summary>
    public Guid? UserId { get; set; }

    public bool IsForcePasswordChange { get; set; } = true;
    public Guid? RefId { get; set; }

    public string FullName => $"{FirstName} {LastName}";
}
