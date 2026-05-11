using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Groups.Commands.CreateGroup;

public record CreateGroupCommand(string GroupName, string? Description, UserRole Role) : IRequest<Guid>;

public class CreateGroupCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateGroupCommand, Guid>
{
    public async Task<Guid> Handle(CreateGroupCommand request, CancellationToken ct)
    {
        var group = new SecurityGroup
        {
            GroupName = request.GroupName,
            Description = request.Description,
            Role = request.Role,
            CreatedBy = currentUser.Email
        };
        await uow.SecurityGroups.AddAsync(group, ct);
        await uow.SaveChangesAsync(ct);
        return group.Id;
    }
}
