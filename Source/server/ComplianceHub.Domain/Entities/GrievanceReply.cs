using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class GrievanceReply : BaseEntity
{
    public Guid GrievanceId { get; set; }
    public Grievance? Grievance { get; set; }

    public Guid SenderSecurityUserId { get; set; }
    public SecurityUser? SenderSecurityUser { get; set; }
    public UserRole SenderUserType { get; set; }

    public string Message { get; set; } = string.Empty;
}
