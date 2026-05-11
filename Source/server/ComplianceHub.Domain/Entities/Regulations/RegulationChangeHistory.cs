using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationChangeHistory : BaseEntity
{
    public Guid RegulationId { get; set; }
    public Regulation? Regulation { get; set; }

    public string SectionNumber { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int Version { get; set; }
    public RegulationChangeType ChangeType { get; set; }
    public DateTime ArchivedAt { get; set; } = DateTime.UtcNow;
}
