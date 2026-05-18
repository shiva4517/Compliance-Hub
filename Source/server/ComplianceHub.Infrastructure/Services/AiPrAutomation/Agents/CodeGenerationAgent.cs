using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Infrastructure.Services.AiPrAutomation.Agents;

internal sealed class CodeGenerationAgent(IAiPrModelProviderFactory providerFactory) : ICodeGenerationAgent
{
    public Task<CodeGenerationResult> ExecuteAsync(StartAiPrAutomationRequest request, CodeGenerationPlan plan, string repositoryPath, CancellationToken ct)
        => providerFactory.Create(request.AiProvider).GenerateCodeChangesAsync(request, plan, repositoryPath, ct);
}
