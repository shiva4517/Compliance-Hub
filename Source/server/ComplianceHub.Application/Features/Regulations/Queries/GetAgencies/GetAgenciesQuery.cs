using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetAgencies;

public record GetAgenciesQuery(Guid GovernmentEntityId) : IRequest<IReadOnlyList<AgencyHierarchyDto>>;

public class GetAgenciesQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetAgenciesQuery, IReadOnlyList<AgencyHierarchyDto>>
{
    public async Task<IReadOnlyList<AgencyHierarchyDto>> Handle(GetAgenciesQuery request, CancellationToken ct)
    {
        var rows = await uow.Agencies.Query()
            .IgnoreQueryFilters()
            .Where(a => a.GovernmentEntityId == request.GovernmentEntityId && !a.IsDeleted)
            .OrderBy(a => a.ChapterNumber)
            .Select(a => new { a.Id, a.ChapterNumber, a.AgencyName })
            .ToListAsync(ct);

        return rows.Select(a => new AgencyHierarchyDto(a.Id, a.ChapterNumber, a.AgencyName, [])).ToList();
    }
}
