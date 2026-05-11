using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationSubtype : RegulationBaseEntity
{
    public string SubpartIdentifier { get; set; } = string.Empty;
    public string SubpartName { get; set; } = string.Empty;

    public Guid RegulationTypeId { get; set; }
    public RegulationType? RegulationType { get; set; }

    public ICollection<Regulation> Regulations { get; set; } = [];
}
