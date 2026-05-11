using ComplianceHub.Domain.Entities.Common;

namespace ComplianceHub.Domain.Entities.Regulations;

public class GovernmentEntity : RegulationBaseEntity
{
    public int TitleNumber { get; set; }
    public string TitleName { get; set; } = string.Empty;
    public string? Source { get; set; }
    public bool IsSyncEnabled { get; set; } = false;
    public bool IsImported { get; set; } = false;
    public DateOnly? LastAmendedDate { get; set; }
    public DateTime? LastSyncedDate { get; set; }

    public ICollection<Agency> Agencies { get; set; } = [];
}
