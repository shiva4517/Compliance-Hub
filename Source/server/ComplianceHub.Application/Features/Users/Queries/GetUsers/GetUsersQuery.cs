using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Users.Queries.GetUsers;

public record GetUsersQuery(string? Search, UserRole? Role, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<UserDto>>;

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Email,
    string? PhoneNumber,
    string? Title,
    UserRole Role,
    string RoleName,
    bool IsActive,
    bool IsForcePasswordChange,
    DateTime? LastLoginAt,
    Guid? SecurityGroupId,
    string? GroupName,
    Guid? UserId,
    Guid? RefId,
    DateTime CreatedAt
);

public class GetUsersQueryHandler(IUnitOfWork uow) : IRequestHandler<GetUsersQuery, PaginatedList<UserDto>>
{
    public async Task<PaginatedList<UserDto>> Handle(GetUsersQuery request, CancellationToken ct)
    {
        var query = uow.SecurityUsers.Query()
            .Include(u => u.SecurityGroup)
            .Where(u => !u.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(s) ||
                u.LastName.ToLower().Contains(s) ||
                u.Email.ToLower().Contains(s));
        }

        if (request.Role.HasValue)
            query = query.Where(u => u.Role == request.Role.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserDto(
                u.Id, u.FirstName, u.LastName, u.FirstName + " " + u.LastName,
                u.Email, u.PhoneNumber, u.Title, u.Role, u.Role.ToString(),
                u.IsActive, u.IsForcePasswordChange, u.LastLoginAt,
                u.SecurityGroupId, u.SecurityGroup != null ? u.SecurityGroup.GroupName : null,
                u.UserId, u.RefId, u.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<UserDto> { Items = items, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize };
    }
}
