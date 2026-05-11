using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationType : RegulationBaseEntity
{
    public int PartNumber { get; set; }
    public string PartName { get; set; } = string.Empty;

    public Guid GovernmentEntityId { get; set; }
    public GovernmentEntity? GovernmentEntity { get; set; }

    public Guid AgencyId { get; set; }
    public Agency? Agency { get; set; }

    public Guid RegulationCategoryId { get; set; }
    public RegulationCategory? RegulationCategory { get; set; }

    public ICollection<RegulationSubtype> RegulationSubtypes { get; set; } = [];
    public ICollection<Regulation> Regulations { get; set; } = [];
}
