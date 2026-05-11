using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetSubtypes;

public record GetSubtypesQuery(Guid TypeId) : IRequest<IReadOnlyList<SubtypeHierarchyDto>>;

public class GetSubtypesQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetSubtypesQuery, IReadOnlyList<SubtypeHierarchyDto>>
{
    public async Task<IReadOnlyList<SubtypeHierarchyDto>> Handle(GetSubtypesQuery request, CancellationToken ct)
    {
        var rows = await uow.RegulationSubtypes.Query()
            .IgnoreQueryFilters()
            .Where(s => s.RegulationTypeId == request.TypeId && !s.IsDeleted)
            .OrderBy(s => s.SubpartIdentifier)
            .Select(s => new { s.Id, s.SubpartIdentifier, s.SubpartName })
            .ToListAsync(ct);

        var subtypes = rows.Select(s => new SubtypeHierarchyDto(s.Id, s.SubpartIdentifier, s.SubpartName, [])).ToList();

        // Include synthetic NO_SUBPART entry if direct sections exist for this type
        var hasDirectSections = await uow.Regulations.Query()
            .IgnoreQueryFilters()
            .AnyAsync(r => r.RegulationTypeId == request.TypeId && r.RegulationSubtypeId == null && r.IsActive, ct);

        if (hasDirectSections)
            subtypes.Add(new SubtypeHierarchyDto(Guid.Empty, "NO_SUBPART", "(No Subpart)", []));

        return subtypes;
    }
}
