using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Regulations.Commands.TriggerManualSync;

public record TriggerManualSyncCommand(Guid Id) : IRequest<SyncResult>;

public class TriggerManualSyncCommandHandler(IRegulationSyncOrchestrator orchestrator)
    : IRequestHandler<TriggerManualSyncCommand, SyncResult>
{
    public async Task<SyncResult> Handle(TriggerManualSyncCommand request, CancellationToken ct)
        => await orchestrator.SyncGovernmentEntityAsync(request.Id, null, "Manual", ct);
}
