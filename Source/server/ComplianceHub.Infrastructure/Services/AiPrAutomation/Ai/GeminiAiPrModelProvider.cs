using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Configuration;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;

/// <summary>
/// Google Gemini via its OpenAI-compatible endpoint
/// (https://generativelanguage.googleapis.com/v1beta/openai/).
/// Uses the shared OpenAI chat/completions protocol implementation.
/// </summary>
internal sealed class GeminiAiPrModelProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ProcessCommandRunner commandRunner)
    : OpenAiCompatibleAiPrModelProvider(httpClientFactory, configuration, commandRunner)
{
    public override AiPrModelProviderKind Kind => AiPrModelProviderKind.Gemini;

    protected override string HttpClientName => "AiPrGemini";

    protected override string ConfigPrefix => "AiPrAutomation:Gemini";

    protected override string DefaultModel => "gemini-2.5-flash";

    protected override IReadOnlyList<string> ApiKeyEnvironmentVariables { get; } =
        new[] { "AIPR_GEMINI_API_KEY", "GEMINI_API_KEY" };
}
