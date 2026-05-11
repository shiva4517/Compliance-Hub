using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.FrequencyTypes.Queries;

public record LookupDto(Guid Id, string Name);

public record GetFrequencyTypesQuery : IRequest<List<LookupDto>>;

public class GetFrequencyTypesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetFrequencyTypesQuery, List<LookupDto>>
{
    public async Task<List<LookupDto>> Handle(GetFrequencyTypesQuery request, CancellationToken ct) =>
        await uow.FrequencyTypes.Query()
            .Where(f => f.IsActive)
            .OrderBy(f => f.Name)
            .Select(f => new LookupDto(f.Id, f.Name))
            .ToListAsync(ct);
}
