using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Groups.Queries;

public record GetGroupsQuery : IRequest<List<GroupDto>>;

public record GroupDto(Guid Id, string GroupName, string? Description, UserRole Role, string RoleName, bool IsActive, int UserCount, DateTime CreatedAt);

public class GetGroupsQueryHandler(IUnitOfWork uow) : IRequestHandler<GetGroupsQuery, List<GroupDto>>
{
    public async Task<List<GroupDto>> Handle(GetGroupsQuery request, CancellationToken ct)
    {
        var groups = await uow.SecurityGroups.Query()
            .Where(g => !g.IsDeleted)
            .Include(g => g.Users)
            .OrderBy(g => g.GroupName)
            .ToListAsync(ct);

        return groups
            .Select(g => new GroupDto(g.Id, g.GroupName, g.Description, g.Role, g.Role.ToString(),
                g.IsActive, g.Users.Count(u => !u.IsDeleted), g.CreatedAt))
            .ToList();
    }
}
