using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.RegulationDetails;
using MediatR;

namespace ComplianceHub.Application.Features.RegulationDetails.Queries.GetAiProviderModels;

public record GetAiProviderModelsQuery(
    string Provider,
    string ApiKey,
    string? Endpoint) : IRequest<IReadOnlyList<AiProviderModelOptionDto>>;

public record AiProviderModelOptionDto(
    string Value,
    string Label);

public class GetAiProviderModelsQueryHandler(
    IAiSuggestionService aiSuggestionService)
    : IRequestHandler<GetAiProviderModelsQuery, IReadOnlyList<AiProviderModelOptionDto>>
{
    public async Task<IReadOnlyList<AiProviderModelOptionDto>> Handle(GetAiProviderModelsQuery request, CancellationToken ct)
    {
        var provider = AiProviderMetadata.NormalizeProvider(request.Provider);

        if (string.IsNullOrWhiteSpace(request.ApiKey))
            throw new ArgumentException("API key is required to load models.");

        if (provider == "azure-openai" && string.IsNullOrWhiteSpace(request.Endpoint))
            throw new ArgumentException("Azure OpenAI endpoint is required to load models.");

        var models = await aiSuggestionService.GetAvailableModelsAsync(
            new AiProviderConnectionSettings(
                provider,
                request.ApiKey.Trim(),
                request.Endpoint?.Trim(),
                null,
                null,
                null),
            ct);

        return models
            .Select(model => new AiProviderModelOptionDto(model.Value, model.Label))
            .ToArray();
    }
}
