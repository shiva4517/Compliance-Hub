using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Agent.Exceptions;
using ComplianceHub.Infrastructure.Services.Ai.LlmClients;
using Microsoft.EntityFrameworkCore;
using ComplianceHub.Infrastructure.Data;

namespace ComplianceHub.Infrastructure.Services.Ai;

public class LlmClientFactory(
    ComplianceHubDbContext db,
    IAiProviderSecretProtector protector,
    IHttpClientFactory httpClientFactory) : ILlmClientFactory
{
    public async Task<ILlmClient> CreateForUserAsync(Guid securityUserId, CancellationToken ct)
    {
        var connection = await db.AiProviderConnections
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SecurityUserId == securityUserId, ct);

        if (connection is null)
            throw new AiProviderNotConfiguredException();

        var apiKey = protector.Unprotect(connection.EncryptedApiKey);
        var model = connection.Model ?? string.Empty;

        return connection.Provider switch
        {
            "azure-openai" => new AzureOpenAiLlmClient(
                httpClientFactory,
                apiKey,
                connection.Endpoint ?? throw new InvalidOperationException("Azure endpoint missing."),
                string.IsNullOrWhiteSpace(connection.DeploymentName) ? model : connection.DeploymentName,
                connection.ApiVersion ?? "2024-02-15-preview"),

            "openai" => new OpenAiLlmClient(httpClientFactory, apiKey, model),

            "gemini" => new GeminiLlmClient(httpClientFactory, apiKey, model),

            "claude" => new AnthropicClaudeLlmClient(httpClientFactory, apiKey, model),

            "openrouter" => new OpenRouterLlmClient(httpClientFactory, apiKey, model),

            _ => throw new InvalidOperationException($"Unsupported AI provider: {connection.Provider}")
        };
    }
}
