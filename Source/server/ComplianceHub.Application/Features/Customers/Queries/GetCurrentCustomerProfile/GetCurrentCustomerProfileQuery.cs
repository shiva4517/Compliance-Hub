using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Companies.Queries.GetCompanies;
using ComplianceHub.Application.Features.Customers.Queries.GetCustomers;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Queries.GetCurrentCustomerProfile;

public record CurrentCustomerProfileDto(
    CustomerDto Customer,
    CompanyDto Company
);

public record GetCurrentCustomerProfileQuery(Guid SecurityUserId) : IRequest<CurrentCustomerProfileDto>;

public class GetCurrentCustomerProfileQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetCurrentCustomerProfileQuery, CurrentCustomerProfileDto>
{
    public async Task<CurrentCustomerProfileDto> Handle(GetCurrentCustomerProfileQuery request, CancellationToken ct)
    {
        var securityUser = await uow.SecurityUsers.Query()
            .FirstOrDefaultAsync(u => u.Id == request.SecurityUserId, ct);

        if (securityUser == null)
        {
            var users = await uow.SecurityUsers.FindAsync(x => x.UserId == request.SecurityUserId, ct);
            securityUser = users.FirstOrDefault();

        }

        if(securityUser == null)
        {

            throw new NotFoundException(nameof(Domain.Entities.SecurityUser), request.SecurityUserId);
        }

        if (securityUser.Role != UserRole.Customer || securityUser.UserId is not Guid customerId)
            throw new NotFoundException(nameof(Domain.Entities.Customer), request.SecurityUserId);

        var customer = await uow.Customers.Query()
            .Include(c => c.Company)
            .FirstOrDefaultAsync(c => c.Id == customerId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), customerId);

        var customerDto = new CustomerDto(
            customer.Id,
            customer.CompanyId,
            customer.Company.CompanyName,
            customer.CustomerCode,
            customer.CustomerName,
            customer.PrimaryContactFirstName,
            customer.PrimaryContactLastName,
            customer.PrimaryEmail,
            customer.SecondaryEmail,
            customer.PhoneNumber,
            customer.MobileNumber,
            customer.PrimaryAddress,
            customer.PrimaryCity,
            customer.PrimaryState,
            customer.PrimaryPostalCode,
            customer.SecondaryAddress,
            customer.SecondaryCity,
            customer.SecondaryState,
            customer.SecondaryPostalCode,
            customer.IsActive,
            customer.CreatedAt);

        var companyDto = new CompanyDto(
            customer.Company.Id,
            customer.Company.CompanyCode,
            customer.Company.CompanyName,
            customer.Company.PrimaryEmail,
            customer.Company.SecondaryEmail,
            customer.Company.PhoneNumber,
            customer.Company.PrimaryAddress,
            customer.Company.PrimaryCity,
            customer.Company.PrimaryState,
            customer.Company.PrimaryPostalCode,
            customer.Company.SecondaryAddress,
            customer.Company.SecondaryCity,
            customer.Company.SecondaryState,
            customer.Company.SecondaryPostalCode,
            customer.Company.WebsiteUrl,
            customer.Company.IsActive,
            customer.Company.CreatedAt);

        return new CurrentCustomerProfileDto(customerDto, companyDto);
    }
}
