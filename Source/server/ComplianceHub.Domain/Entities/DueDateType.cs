using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities
{
    public class DueDateType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}