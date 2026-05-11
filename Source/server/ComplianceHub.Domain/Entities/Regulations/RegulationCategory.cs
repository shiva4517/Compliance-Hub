using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationCategory : RegulationBaseEntity
{
    public string SubchapterIdentifier { get; set; } = string.Empty;
    public string SubchapterName { get; set; } = string.Empty;

    public Guid GovernmentEntityId { get; set; }
    public GovernmentEntity? GovernmentEntity { get; set; }

    public Guid AgencyId { get; set; }
    public Agency? Agency { get; set; }

    public ICollection<RegulationType> RegulationTypes { get; set; } = [];
}
