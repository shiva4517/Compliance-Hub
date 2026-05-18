using ComplianceHub.Application.Common.Interfaces.AiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using MediatR;

namespace ComplianceHub.Application.Features.AiPrAutomation.Commands.StartAiPrAutomation;

public sealed record StartAiPrAutomationCommand(StartAiPrAutomationRequest Request) : IRequest<AiPrAutomationRunResult>;

public sealed class StartAiPrAutomationCommandHandler(IAiPrWorkflowEngine workflowEngine)
    : IRequestHandler<StartAiPrAutomationCommand, AiPrAutomationRunResult>
{
    public Task<AiPrAutomationRunResult> Handle(StartAiPrAutomationCommand request, CancellationToken cancellationToken)
        => workflowEngine.RunAsync(request.Request, cancellationToken);
}
