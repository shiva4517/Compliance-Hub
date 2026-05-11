using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class Agency : RegulationBaseEntity
{
    public string ChapterNumber { get; set; } = string.Empty;
    public string AgencyName { get; set; } = string.Empty;

    public Guid GovernmentEntityId { get; set; }
    public GovernmentEntity? GovernmentEntity { get; set; }

    public ICollection<RegulationCategory> RegulationCategories { get; set; } = [];
}
