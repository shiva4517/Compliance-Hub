using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using MediatR;

namespace ComplianceHub.Application.Features.Groups.Commands.DeleteGroup;

public record DeleteGroupCommand(Guid Id) : IRequest;

public class DeleteGroupCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteGroupCommand>
{
    public async Task Handle(DeleteGroupCommand request, CancellationToken ct)
    {
        var group = await uow.SecurityGroups.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.SecurityGroup), request.Id);

        group.IsDeleted = true;
        group.UpdatedAt = DateTime.UtcNow;
        uow.SecurityGroups.Update(group);
        await uow.SaveChangesAsync(ct);
    }
}
