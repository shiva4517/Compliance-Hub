using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class RegulationChangeLog : BaseEntity
{
    public Guid RegulationId { get; set; }
    public Regulation? Regulation { get; set; }

    // Regulation hierarchy snapshot — no cross-DB joins at query time
    public Guid GovernmentEntityId { get; set; }
    public string GovernmentEntityName { get; set; } = string.Empty;
    public Guid AgencyId { get; set; }
    public string AgencyName { get; set; } = string.Empty;
    public Guid RegulationCategoryId { get; set; }
    public string RegulationCategoryName { get; set; } = string.Empty;
    public Guid RegulationTypeId { get; set; }
    public string RegulationTypeName { get; set; } = string.Empty;
    public Guid? RegulationSubtypeId { get; set; }
    public string? RegulationSubtypeName { get; set; }

    // Section identity
    public string SectionNumber { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;

    // Content hashes for audit traceability
    public string? PreviousContentHash { get; set; }
    public string? NewContentHash { get; set; }

    // Full HTML content — not returned in list queries
    public string? PreviousHtmlContent { get; set; }
    public string? NewHtmlContent { get; set; }

    // Amendment date snapshots
    public DateOnly? PreviousAmendedDate { get; set; }
    public DateOnly? NewAmendedDate { get; set; }

    // Version tracking
    public int ArchivedVersion { get; set; }
    public int NewVersion { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
