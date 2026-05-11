namespace ComplianceHub.Application.Common.Interfaces;

public interface IAiSuggestionService
{
    Task<RegulationSuggestionResult> SuggestRegulationDetailAsync(
        RegulationSuggestionContext context,
        AiProviderConnectionSettings connection,
        CancellationToken ct);

    Task<IReadOnlyList<AiProviderModelOption>> GetAvailableModelsAsync(
        AiProviderConnectionSettings connection,
        CancellationToken ct);
}

public record RegulationSuggestionContext(
    Guid RegulationId,
    string Provider,
    string TitleName,
    string SectionNumber,
    string SectionName,
    string? ExistingDescription,
    string? ExistingCondition,
    string? ExistingSuggestedTask,
    string? HtmlContent,
    // Subscription-level context (null for the legacy regulation-only flow).
    string? SubscribedNodeName = null,
    string? SubscribingLevel = null,
    decimal? ExistingMinValue = null,
    decimal? ExistingMaxValue = null);

public record AiProviderConnectionSettings(
    string Provider,
    string ApiKey,
    string? Endpoint,
    string? DeploymentName,
    string? Model,
    string? ApiVersion);

public record RegulationSuggestionResult(
    string Description,
    string Condition,
    string SuggestedTask,
    string FrequencyType,
    string DueDateType,
    string Provider,
    decimal? MinValue,
    decimal? MaxValue);

public record AiProviderModelOption(
    string Value,
    string Label);
