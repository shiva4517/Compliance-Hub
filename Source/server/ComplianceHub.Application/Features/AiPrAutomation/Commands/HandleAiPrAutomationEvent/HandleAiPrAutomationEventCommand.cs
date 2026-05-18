using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using MediatR;

namespace ComplianceHub.Application.Features.AiPrAutomation.Commands.HandleAiPrAutomationEvent;

public sealed record HandleAiPrAutomationEventCommand(AiPrAutomationEvent AutomationEvent)
    : IRequest<AiPrAutomationEventResult>;

public sealed class HandleAiPrAutomationEventCommandHandler(IAiPrEventOrchestrator orchestrator)
    : IRequestHandler<HandleAiPrAutomationEventCommand, AiPrAutomationEventResult>
{
    public Task<AiPrAutomationEventResult> Handle(HandleAiPrAutomationEventCommand request, CancellationToken cancellationToken)
        => orchestrator.HandleAsync(request.AutomationEvent, cancellationToken);
}
