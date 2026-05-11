using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Groups.Commands.UpdateGroup;

public record UpdateGroupCommand(Guid Id, string GroupName, string? Description, UserRole Role, bool IsActive) : IRequest;

public class UpdateGroupCommandHandler(IUnitOfWork uow) : IRequestHandler<UpdateGroupCommand>
{
    public async Task Handle(UpdateGroupCommand request, CancellationToken ct)
    {
        var group = await uow.SecurityGroups.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.SecurityGroup), request.Id);

        group.GroupName = request.GroupName;
        group.Description = request.Description;
        group.Role = request.Role;
        group.IsActive = request.IsActive;
        group.UpdatedAt = DateTime.UtcNow;
        uow.SecurityGroups.Update(group);
        await uow.SaveChangesAsync(ct);
    }
}
