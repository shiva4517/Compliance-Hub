using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.CompanyDivisions.Queries.GetCompanyDivisions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.CompanyDivisions.Queries.GetCompanyDivisionById;

public record GetCompanyDivisionByIdQuery(Guid Id) : IRequest<CompanyDivisionDto>;

public class GetCompanyDivisionByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCompanyDivisionByIdQuery, CompanyDivisionDto>
{
    public async Task<CompanyDivisionDto> Handle(GetCompanyDivisionByIdQuery request, CancellationToken ct)
    {
        var d = await uow.CompanyDivisions.Query()
            .Include(x => x.Company)
            .Include(x => x.Department)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyDivision), request.Id);

        return new CompanyDivisionDto(d.Id, d.CompanyId, d.Company.CompanyName,
            d.DepartmentId, d.Department?.Name,
            d.Name, d.Description, d.IsActive, d.CreatedAt);
    }
}
