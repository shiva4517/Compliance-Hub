using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetMyStats;

public record GetMyStatsQuery(Guid EmployeeId) : IRequest<MyStatsDto>;

public record MyStatsDto(int TotalAssignments, int ActiveAssignments, int TotalChangeNotices);

public class GetMyStatsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetMyStatsQuery, MyStatsDto>
{
    public async Task<MyStatsDto> Handle(GetMyStatsQuery request, CancellationToken ct)
    {
        var allAssignments = await uow.EmployeeAssignedWorks.Query()
            .Where(a => a.EmployeeId == request.EmployeeId)
            .Select(a => new { a.IsActive,a.IsDeleted})
            .ToListAsync(ct);

        var assignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Subscription)
            .Include(a => a.Customer)
            .Where(a => a.EmployeeId == request.EmployeeId)
            .OrderByDescending(a => a.AssignedAt)
            .ToListAsync(ct);

        var totalAssignments = allAssignments.Count;
        var activeAssignments = assignments.Count(a => a.IsActive && !a.IsDeleted);

        return new MyStatsDto(totalAssignments, activeAssignments, 0);
    }
}
