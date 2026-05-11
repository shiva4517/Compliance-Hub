using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetTypes;

public record GetTypesQuery(Guid CategoryId) : IRequest<IReadOnlyList<TypeHierarchyDto>>;

public class GetTypesQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetTypesQuery, IReadOnlyList<TypeHierarchyDto>>
{
    public async Task<IReadOnlyList<TypeHierarchyDto>> Handle(GetTypesQuery request, CancellationToken ct)
    {
        var rows = await uow.RegulationTypes.Query()
            .IgnoreQueryFilters()
            .Where(t => t.RegulationCategoryId == request.CategoryId && !t.IsDeleted)
            .OrderBy(t => t.PartNumber)
            .Select(t => new { t.Id, t.PartNumber, t.PartName })
            .ToListAsync(ct);

        return rows.Select(t => new TypeHierarchyDto(t.Id, t.PartNumber, t.PartName, [], [])).ToList();
    }
}
  