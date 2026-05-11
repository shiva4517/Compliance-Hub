using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.FrequencyTypes.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.DueDateTypes.Queries;

public record GetDueDateTypesQuery : IRequest<List<LookupDto>>;

public class GetDueDateTypesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDueDateTypesQuery, List<LookupDto>>
{
    public async Task<List<LookupDto>> Handle(GetDueDateTypesQuery request, CancellationToken ct) =>
        await uow.DueDateTypes.Query()
            .Where(d => d.IsActive)
            .OrderBy(d => d.Name)
            .Select(d => new LookupDto(d.Id, d.Name))
            .ToListAsync(ct);
}
