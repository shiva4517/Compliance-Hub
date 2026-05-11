using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Districts.Queries.GetDistricts;

public record GetDistrictsQuery(Guid CompanyId, string? Search, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<DistrictDto>>;

public record DistrictDto(
    Guid Id,
    Guid CompanyId,
    string Name,
    string? Description,
    bool IsActive,
    DateTime CreatedAt
);

public class GetDistrictsQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDistrictsQuery, PaginatedList<DistrictDto>>
{
    public async Task<PaginatedList<DistrictDto>> Handle(GetDistrictsQuery request, CancellationToken ct)
    {
        IQueryable<Domain.Entities.District> query = uow.Districts.Query()
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
            .Select(d => new DistrictDto(d.Id, d.CompanyId, d.Name, d.Description, d.IsActive, d.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<DistrictDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
