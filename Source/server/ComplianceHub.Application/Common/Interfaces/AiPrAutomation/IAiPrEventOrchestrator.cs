using ComplianceHub.Application.Features.AiPrAutomation.Models;

namespace ComplianceHub.Application.Common.Interfaces.AiPrAutomation;

public interface IAiPrEventOrchestrator
{
    Task<AiPrAutomationEventResult> HandleAsync(AiPrAutomationEvent automationEvent, CancellationToken ct);
}
