using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Departments.Queries.GetDepartments;

public record GetDepartmentsQuery(Guid CompanyId, string? Search, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<DepartmentDto>>;

public record DepartmentDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    bool IsActive,
    int DivisionCount,
    DateTime CreatedAt
);

public class GetDepartmentsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDepartmentsQuery, PaginatedList<DepartmentDto>>
{
    public async Task<PaginatedList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken ct)
    {
        IQueryable<Domain.Entities.Department> query = uow.Departments.Query()
            .Include(d => d.Divisions)
            .Where(d => d.CompanyId == request.CompanyId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(s) ||
                (d.Description != null && d.Description.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new DepartmentDto(
                d.Id, d.CompanyId, d.Name, d.Description, d.IsActive,
                d.Divisions.Count(),
                d.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<DepartmentDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
