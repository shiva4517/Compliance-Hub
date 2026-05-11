using ComplianceHub.Domain.Entities.Common;
using ComplianceHub.Domain.Enums;

namespace ComplianceHub.Domain.Entities;

public class RegulationDetail : BaseEntity
{
    public Guid RegulationId { get; set; }
    public string? Description { get; set; }
    public string? Condition { get; set; }
    public string? SuggestedTask { get; set; }
    public Guid? FrequencyTypeId { get; set; }
    public FrequencyType? FrequencyType { get; set; }
    public Guid? DueDateTypeId { get; set; }
    public DueDateType? DueDateType { get; set; }

    // Subscription-level rebrand (see migration 20260511100000_AddSubscriptionScopeToRegulationDetails).
    // When a row was authored from the Subscriptions module, SubscriptionId/SubscribingLevel
    // identify the scope; MinValue/MaxValue carry the optional Data Range for Condition.
    // Rows created by the legacy Regulations module have these as NULL — they stay readable
    // but are no longer surfaced in the UI.
    public Guid? SubscriptionId { get; set; }
    public SubscribingLevel? SubscribingLevel { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
}
