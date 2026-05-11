using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Commands.ToggleSyncEnabled;

public record ToggleSyncEnabledCommand(Guid Id) : IRequest<bool>;

public class ToggleSyncEnabledCommandHandler(IRegulationsUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<ToggleSyncEnabledCommand, bool>
{
    public async Task<bool> Handle(ToggleSyncEnabledCommand request, CancellationToken ct)
    {
        var entity = await uow.GovernmentEntities.Query()
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"GovernmentEntity {request.Id} not found.");

        entity.IsSyncEnabled = !entity.IsSyncEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUser.Email;

        uow.GovernmentEntities.Update(entity);
        await uow.SaveChangesAsync(ct);
        return entity.IsSyncEnabled;
    }
}
