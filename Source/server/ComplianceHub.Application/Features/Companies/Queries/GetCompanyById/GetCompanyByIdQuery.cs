using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Companies.Queries.GetCompanies;
using MediatR;

namespace ComplianceHub.Application.Features.Companies.Queries.GetCompanyById;

public record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDto>;

public class GetCompanyByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCompanyByIdQuery, CompanyDto>
{
    public async Task<CompanyDto> Handle(GetCompanyByIdQuery request, CancellationToken ct)
    {
        var c = await uow.Companies.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Company), request.Id);

        return new CompanyDto(
            c.Id, c.CompanyCode, c.CompanyName, c.PrimaryEmail, c.SecondaryEmail,
            c.PhoneNumber, c.PrimaryAddress, c.PrimaryCity, c.PrimaryState, c.PrimaryPostalCode,
            c.SecondaryAddress, c.SecondaryCity, c.SecondaryState, c.SecondaryPostalCode,
            c.WebsiteUrl, c.IsActive, c.CreatedAt);
    }
}
