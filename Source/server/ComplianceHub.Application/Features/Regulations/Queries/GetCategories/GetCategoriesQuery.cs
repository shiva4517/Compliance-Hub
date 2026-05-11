using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetCategories;

public record GetCategoriesQuery(Guid AgencyId) : IRequest<IReadOnlyList<CategoryHierarchyDto>>;

public class GetCategoriesQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryHierarchyDto>>
{
    public async Task<IReadOnlyList<CategoryHierarchyDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var rows = await uow.RegulationCategories.Query()
            .IgnoreQueryFilters()
            .Where(c => c.AgencyId == request.AgencyId && !c.IsDeleted)
            .OrderBy(c => c.SubchapterIdentifier)
            .Select(c => new { c.Id, c.SubchapterIdentifier, c.SubchapterName })
            .ToListAsync(ct);

        return rows.Select(c => new CategoryHierarchyDto(c.Id, c.SubchapterIdentifier, c.SubchapterName, [])).ToList();
    }
}
