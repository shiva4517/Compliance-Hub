using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Users.Commands.UpdateUser;

public record UpdateUserCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? Title,
    UserRole Role,
    bool IsActive,
    Guid? SecurityGroupId,
    Guid? UserId,
    Guid? RefId
) : IRequest;

public class UpdateUserCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateUserCommand>
{
    public async Task Handle(UpdateUserCommand request, CancellationToken ct)
    {
        var user = await uow.SecurityUsers.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.SecurityUser), request.Id);

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;
        user.Title = request.Title;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.SecurityGroupId = request.SecurityGroupId;
        user.UserId = request.UserId;
        user.RefId = request.RefId;
        user.UpdatedAt = DateTime.UtcNow;
        user.UpdatedBy = currentUser.Email;

        uow.SecurityUsers.Update(user);
        await uow.SaveChangesAsync(ct);
    }
}
