namespace ComplianceHub.Functions.Models;

// DB1 — Compliance Hub Regulations DB
public class GovEntityRow
{
    public Guid Id { get; set; }
    public int TitleNumber { get; set; }
    public string TitleName { get; set; } = string.Empty;
    public bool IsSyncEnabled { get; set; }
    public bool IsImported { get; set; }
    public DateTime? LastAmendedDate { get; set; }
    public DateTime? LastSyncedDate { get; set; }
    public DateOnly? LastAmendedDateOnly => LastAmendedDate.HasValue ? DateOnly.FromDateTime(LastAmendedDate.Value) : null;
}
public record AgencyRow(Guid Id, Guid GovernmentEntityId, string ChapterNumber, string AgencyName);
public record CategoryRow(Guid Id, Guid AgencyId, string SubchapterIdentifier, string SubchapterName);
public record TypeRow(Guid Id, Guid RegulationCategoryId, int PartNumber, string PartName);
public record SubtypeRow(Guid Id, Guid RegulationTypeId, string SubpartIdentifier, string SubpartName);
public record RegulationRow(
    Guid Id,
    Guid GovernmentEntityId,
    Guid AgencyId,
    Guid RegulationCategoryId,
    Guid RegulationTypeId,
    Guid? RegulationSubtypeId,
    string SectionNumber,
    string SectionName,
    string HtmlContent,
    string ContentHash,
    int Version,
    bool IsActive,
    DateOnly? LastAmendedDate
);
public class HierarchyDto
{
    // Government Entity
    public Guid GovernmentEntityId { get; set; }
    public string GovernmentEntityName { get; set; } = string.Empty;

    // Agency
    public Guid AgencyId { get; set; }
    public string AgencyName { get; set; } = string.Empty;

    // Regulation Category
    public Guid RegulationCategoryId { get; set; }
    public string RegulationCategoryName { get; set; } = string.Empty;

    // Regulation Type
    public Guid RegulationTypeId { get; set; }
    public string RegulationTypeName { get; set; } = string.Empty;

    // Regulation Subtype (nullable)
    public Guid? RegulationSubtypeId { get; set; }
    public string? RegulationSubtypeName { get; set; }
}
public class CustomerWithCompany
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string PrimaryEmail { get; set; } = string.Empty;
}
public class NotificationHistoryDto
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid SubscriptionId { get; set; }

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

    public Guid PreviousRegulationId { get; set; }
    public string PreviousRegulationName { get; set; } = string.Empty;

    public Guid PresentRegulationId { get; set; }
    public string PresentRegulationName { get; set; } = string.Empty;

    public int Version { get; set; }

    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;

    public string RecipientEmail { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;

    public string Status { get; set; } = "Pending";

    public bool IsNotified { get; set; } = false;
    public int RetryCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; }
}
public class SubscriptionDto
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid GovernmentEntityId { get; set; }
    public Guid? AgencyId { get; set; }
    public Guid? RegulationCategoryId { get; set; }
    public Guid? RegulationTypeId { get; set; }
    public Guid? RegulationSubtypeId { get; set; }
    public Guid? RegulationId { get; set; }

    public int SubscribingLevel { get; set; }  // keep as int for Dapper performance
}
public record OutboxEventRow(Guid Id, string EventType, string Payload, int RetryCount);

// DB2 — Compliance Hub Portal DB
public record SubscriptionRow(Guid Id, Guid CustomerId, Guid GovernmentEntityId, Guid? RegulationId, bool IsActive);
