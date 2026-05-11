using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.RegulationDetails;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.RegulationDetails.Commands.UpsertAiProviderConnection;

public record UpsertAiProviderConnectionCommand(
    string Provider,
    string ApiKey,
    string? Endpoint,
    string? DeploymentName,
    string? Model,
    string? ApiVersion) : IRequest<AiProviderConnectionDto>;

public record AiProviderConnectionDto(
    string Provider,
    string ProviderDisplayName,
    string? Endpoint,
    string? DeploymentName,
    string? Model,
    string? ApiVersion,
    bool IsConfigured);

public class UpsertAiProviderConnectionCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IAiProviderSecretProtector protector,
    IAiSuggestionService aiSuggestionService)
    : IRequestHandler<UpsertAiProviderConnectionCommand, AiProviderConnectionDto>
{
    public async Task<AiProviderConnectionDto> Handle(UpsertAiProviderConnectionCommand request, CancellationToken ct)
    {
        if (currentUser.SecurityUserId is not Guid securityUserId)
        {
            throw new UnauthorizedAccessException("Authenticated user not found.");
        }

        var normalizedProvider = AiProviderMetadata.NormalizeProvider(request.Provider);
        Validate(request, normalizedProvider);
        await ValidateProviderAccessAsync(request, normalizedProvider, ct);

        var existing = await uow.AiProviderConnections.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.SecurityUserId == securityUserId, ct);
        var isNew = existing is null;

        if (isNew)
        {
            existing = new AiProviderConnection
            {
                SecurityUserId = securityUserId,
                CreatedBy = currentUser.Email
            };

            await uow.AiProviderConnections.AddAsync(existing, ct);
        }

        if (existing is null)
        {
            throw new InvalidOperationException("Unable to initialize AI provider connection.");
        }

        existing.Provider = normalizedProvider;
        existing.EncryptedApiKey = protector.Protect(request.ApiKey.Trim());
        var normalizedModel = request.Model?.Trim();
        var normalizedDeploymentName = request.DeploymentName?.Trim();

        existing.Endpoint = request.Endpoint?.Trim();
        existing.DeploymentName = normalizedDeploymentName ?? (normalizedProvider == "azure-openai" ? normalizedModel : null);
        existing.Model = normalizedModel;
        existing.ApiVersion = request.ApiVersion?.Trim();
        existing.IsDeleted = false;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.UpdatedBy = currentUser.Email;

        if (!isNew)
        {
            uow.AiProviderConnections.Update(existing!);
        }

        await uow.SaveChangesAsync(ct);

        return new AiProviderConnectionDto(
            existing.Provider,
            AiProviderMetadata.GetProviderDisplayName(existing.Provider),
            existing.Endpoint,
            existing.DeploymentName,
            existing.Model,
            existing.ApiVersion,
            true);
    }

    private static void Validate(UpsertAiProviderConnectionCommand request, string provider)
    {
        if (string.IsNullOrWhiteSpace(request.ApiKey))
        {
            throw new ArgumentException("API key is required.");
        }

        if (provider == "azure-openai")
        {
            if (string.IsNullOrWhiteSpace(request.Endpoint))
                throw new ArgumentException("Azure OpenAI endpoint is required.");
            if (string.IsNullOrWhiteSpace(request.DeploymentName) && string.IsNullOrWhiteSpace(request.Model))
                throw new ArgumentException("Azure OpenAI model or deployment name is required.");
        }
    }

    private async Task ValidateProviderAccessAsync(UpsertAiProviderConnectionCommand request, string provider, CancellationToken ct)
    {
        var selectedModel = request.Model?.Trim() ?? request.DeploymentName?.Trim();

        var models = await aiSuggestionService.GetAvailableModelsAsync(
            new AiProviderConnectionSettings(
                provider,
                request.ApiKey.Trim(),
                request.Endpoint?.Trim(),
                request.DeploymentName?.Trim(),
                request.Model?.Trim(),
                request.ApiVersion?.Trim()),
            ct);

        if (!string.IsNullOrWhiteSpace(selectedModel) &&
            !models.Any(model => string.Equals(model.Value, selectedModel, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("The selected model is not available for the provided AI provider credentials.");
        }
    }
}
