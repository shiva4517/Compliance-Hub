using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities
{
    public class FrequencyType : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; } = true;
    }
}