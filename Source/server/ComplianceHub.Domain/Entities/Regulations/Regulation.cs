using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class Regulation : RegulationBaseEntity
{
    public string SectionNumber { get; set; } = string.Empty;
    public string SectionName { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateOnly? LastAmendedDate { get; set; }

    public Guid GovernmentEntityId { get; set; }
    public GovernmentEntity? GovernmentEntity { get; set; }

    public Guid AgencyId { get; set; }
    public Agency? Agency { get; set; }

    public Guid RegulationCategoryId { get; set; }
    public RegulationCategory? RegulationCategory { get; set; }

    public Guid RegulationTypeId { get; set; }
    public RegulationType? RegulationType { get; set; }

    public Guid? RegulationSubtypeId { get; set; }
    public RegulationSubtype? RegulationSubtype { get; set; }

    public ICollection<RegulationChangeHistory> ChangeHistory { get; set; } = [];
}
