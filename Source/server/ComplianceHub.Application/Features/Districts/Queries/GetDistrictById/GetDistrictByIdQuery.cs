using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Districts.Queries.GetDistricts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Districts.Queries.GetDistrictById;

public record GetDistrictByIdQuery(Guid Id) : IRequest<DistrictDto>;

public class GetDistrictByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDistrictByIdQuery, DistrictDto>
{
    public async Task<DistrictDto> Handle(GetDistrictByIdQuery request, CancellationToken ct)
    {
        var d = await uow.Districts.Query()
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.District), request.Id);

        return new DistrictDto(d.Id, d.CompanyId, d.Name, d.Description, d.IsActive, d.CreatedAt);
    }
}
