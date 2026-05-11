using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Features.Customers.Queries.GetCustomers;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Queries.GetCustomerById;

public record GetCustomerByIdQuery(Guid Id, Guid CompanyId) : IRequest<CustomerDto>;

public class GetCustomerByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCustomerByIdQuery, CustomerDto>
{
    public async Task<CustomerDto> Handle(GetCustomerByIdQuery request, CancellationToken ct)
    {
        var c = await uow.Customers.Query()
            .Include(x => x.Company)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.CompanyId == request.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        return new CustomerDto(
            c.Id, c.CompanyId, c.Company.CompanyName, c.CustomerCode, c.CustomerName,
            c.PrimaryContactFirstName, c.PrimaryContactLastName, c.PrimaryEmail,
            c.SecondaryEmail, c.PhoneNumber, c.MobileNumber,
            c.PrimaryAddress, c.PrimaryCity, c.PrimaryState, c.PrimaryPostalCode,
            c.SecondaryAddress, c.SecondaryCity, c.SecondaryState, c.SecondaryPostalCode,
            c.IsActive, c.CreatedAt);
    }
}
