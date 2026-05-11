using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.CompanyDivisions.Queries.GetCompanyDivisions;

public record GetCompanyDivisionsQuery(Guid? CompanyId, string? Search, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<CompanyDivisionDto>>;

public record CompanyDivisionDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    Guid? DepartmentId,
    string? DepartmentName,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

public class GetCompanyDivisionsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCompanyDivisionsQuery, PaginatedList<CompanyDivisionDto>>
{
    public async Task<PaginatedList<CompanyDivisionDto>> Handle(GetCompanyDivisionsQuery request, CancellationToken ct)
    {
        IQueryable<Domain.Entities.CompanyDivision> query = uow.CompanyDivisions.Query()
            .Include(d => d.Company)
            .Include(d => d.Department);

        if (request.CompanyId.HasValue)
            query = query.Where(d => d.CompanyId == request.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(d =>
                d.Name.ToLower().Contains(s) ||
                d.Company.CompanyName.ToLower().Contains(s) ||
                (d.Department != null && d.Department.Name.ToLower().Contains(s)) ||
                (d.Description != null && d.Description.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(d => d.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(d => new CompanyDivisionDto(
                d.Id, d.CompanyId, d.Company.CompanyName,
                d.DepartmentId, d.Department != null ? d.Department.Name : null,
                d.Name, d.Description, d.IsActive, d.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<CompanyDivisionDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
