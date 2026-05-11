namespace ComplianceHub.Domain.Entities;

public class ChangeNotice
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RegulationId { get; set; }
    public Guid GovernmentEntityId { get; set; }
    public string GovernmentEntityName { get; set; } = string.Empty;
    public Guid? AgencyId { get; set; }
    public string AgencyName { get; set; } = string.Empty;
    public Guid? RegulationCategoryId { get; set; }
    public string RegulationCategoryName { get; set; } = string.Empty;
    public Guid? RegulationTypeId { get; set; }
    public string RegulationTypeName { get; set; } = string.Empty;
    public Guid? RegulationSubtypeId { get; set; }
    public string? RegulationSubtypeName { get; set; }

    public string SectionNumber { get; set; } = string.Empty;
    public string SectionTitle { get; set; } = string.Empty;

    public string? PreviousContentHash { get; set; }
    public string? NewContentHash { get; set; }
    public string? PreviousHtmlContent { get; set; }
    public string? NewHtmlContent { get; set; }

    public DateOnly? PreviousAmendedDate { get; set; }
    public DateOnly? NewAmendedDate { get; set; }

    public int ArchivedVersion { get; set; }
    public int NewVersion { get; set; }
    public int Version { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
}
