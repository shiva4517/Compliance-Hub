using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Features.Users.Queries.GetUsers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Users.Queries.GetUserById;

public record GetUserByIdQuery(Guid Id) : IRequest<UserDto>;

public class GetUserByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken ct)
    {
        var u = await uow.SecurityUsers.Query()
            .Include(u => u.SecurityGroup)
            .FirstOrDefaultAsync(u => u.Id == request.Id && !u.IsDeleted, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.SecurityUser), request.Id);

        return new UserDto(u.Id, u.FirstName, u.LastName, u.FullName, u.Email, u.PhoneNumber, u.Title,
            u.Role, u.Role.ToString(), u.IsActive, u.IsForcePasswordChange, u.LastLoginAt,
            u.SecurityGroupId, u.SecurityGroup?.GroupName,
            u.UserId, u.RefId, u.CreatedAt);
    }
}
