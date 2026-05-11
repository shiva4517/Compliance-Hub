using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetSections;

public record GetSectionsQuery(Guid SubtypeId, Guid? TypeId = null) : IRequest<IReadOnlyList<SectionDto>>;

public class GetSectionsQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetSectionsQuery, IReadOnlyList<SectionDto>>
{
    public async Task<IReadOnlyList<SectionDto>> Handle(GetSectionsQuery request, CancellationToken ct)
    {
        // SubtypeId == Guid.Empty is the NO_SUBPART sentinel — fetch direct sections for the type
        var query = uow.Regulations.Query()
            .IgnoreQueryFilters()
            .Where(r => r.IsActive && !r.IsDeleted);

        if (request.SubtypeId == Guid.Empty)
            query = query.Where(r => r.RegulationSubtypeId == null && r.RegulationTypeId == request.TypeId);
        else
            query = query.Where(r => r.RegulationSubtypeId == request.SubtypeId);

        return await query
            .OrderBy(r => r.SectionNumber)
            .Select(r => new SectionDto(r.Id, r.SectionNumber, r.SectionName, r.Version, r.IsActive, r.LastAmendedDate, null))
            .ToListAsync(ct);
    }
}
