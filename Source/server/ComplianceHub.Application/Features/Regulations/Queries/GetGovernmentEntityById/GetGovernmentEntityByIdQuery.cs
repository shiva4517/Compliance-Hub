using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Regulations.Queries.GetGovernmentEntities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Queries.GetGovernmentEntityById;

public record GetGovernmentEntityByIdQuery(Guid Id) : IRequest<GovernmentEntityDto?>;

public class GetGovernmentEntityByIdQueryHandler(IRegulationsUnitOfWork uow)
    : IRequestHandler<GetGovernmentEntityByIdQuery, GovernmentEntityDto?>
{
    public async Task<GovernmentEntityDto?> Handle(GetGovernmentEntityByIdQuery request, CancellationToken ct)
    {
        return await uow.GovernmentEntities.Query()
            .Where(e => e.Id == request.Id)
            .Select(e => new GovernmentEntityDto(e.Id, e.TitleNumber, e.TitleName, e.Source,
                e.IsSyncEnabled, e.IsImported, e.LastAmendedDate, e.LastSyncedDate, e.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }
}
