using ComplianceHub.Application.Features.AiPrAutomation.Models;
using ComplianceHub.Infrastructure.Services.AiPrAutomation.Execution;
using Microsoft.Extensions.Configuration;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Ai;

internal sealed class OpenAiAiPrModelProvider(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ProcessCommandRunner commandRunner)
    : OpenAiCompatibleAiPrModelProvider(httpClientFactory, configuration, commandRunner)
{
    public override AiPrModelProviderKind Kind => AiPrModelProviderKind.OpenAI;

    protected override string HttpClientName => "AiPrOpenAi";

    protected override string ConfigPrefix => "AiPrAutomation:OpenAI";

    protected override string DefaultModel => "gpt-4o";

    protected override IReadOnlyList<string> ApiKeyEnvironmentVariables { get; } =
        new[] { "AIPR_OPENAI_API_KEY", "OPENAI_API_KEY" };
}
