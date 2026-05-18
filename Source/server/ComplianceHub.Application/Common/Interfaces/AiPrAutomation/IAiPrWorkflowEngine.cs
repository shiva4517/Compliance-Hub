using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IAiPrWorkflowEngine
{
    Task<AiPrAutomationRunResult> RunAsync(StartAiPrAutomationRequest request, CancellationToken ct);
}
