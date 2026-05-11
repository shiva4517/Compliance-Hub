using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Commands.UpdateGovernmentEntity;

public record UpdateGovernmentEntityCommand(
    Guid Id,
    string TitleName,
    string? Source,
    bool IsSyncEnabled) : IRequest;

public class UpdateGovernmentEntityCommandHandler(IRegulationsUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateGovernmentEntityCommand>
{
    public async Task Handle(UpdateGovernmentEntityCommand request, CancellationToken ct)
    {
        var entity = await uow.GovernmentEntities.Query()
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new KeyNotFoundException($"GovernmentEntity {request.Id} not found.");

        entity.TitleName = request.TitleName;
        entity.Source = request.Source;
        entity.IsSyncEnabled = request.IsSyncEnabled;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUser.Email;

        uow.GovernmentEntities.Update(entity);
        await uow.SaveChangesAsync(ct);
    }
}
