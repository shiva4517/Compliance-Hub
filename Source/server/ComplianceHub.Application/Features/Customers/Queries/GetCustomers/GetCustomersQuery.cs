using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Queries.GetCustomers;

public record GetCustomersQuery(
    Guid CompanyId,
    string? Search,
    int PageNumber = 1,
    int PageSize = 20
) : IRequest<PaginatedList<CustomerDto>>;

public record CustomerDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string CustomerCode,
    string CustomerName,
    string PrimaryContactFirstName,
    string PrimaryContactLastName,
    string PrimaryEmail,
    string? SecondaryEmail,
    string? PhoneNumber,
    string? MobileNumber,
    string? PrimaryAddress,
    string? PrimaryCity,
    string? PrimaryState,
    string? PrimaryPostalCode,
    string? SecondaryAddress,
    string? SecondaryCity,
    string? SecondaryState,
    string? SecondaryPostalCode,
    bool IsActive,
    DateTime CreatedAt
);

public class GetCustomersQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCustomersQuery, PaginatedList<CustomerDto>>
{
    public async Task<PaginatedList<CustomerDto>> Handle(GetCustomersQuery request, CancellationToken ct)
    {
        var query = uow.Customers.Query()
            .Include(c => c.Company)
            .Where(c => c.CompanyId == request.CompanyId);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(c =>
                c.CustomerName.ToLower().Contains(s) ||
                c.CustomerCode.ToLower().Contains(s) ||
                c.PrimaryContactFirstName.ToLower().Contains(s) ||
                c.PrimaryContactLastName.ToLower().Contains(s) ||
                c.PrimaryEmail.ToLower().Contains(s) ||
                (c.SecondaryEmail != null && c.SecondaryEmail.ToLower().Contains(s)) ||
                (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(s)) ||
                (c.MobileNumber != null && c.MobileNumber.ToLower().Contains(s)) ||
                (c.PrimaryAddress != null && c.PrimaryAddress.ToLower().Contains(s)) ||
                (c.PrimaryCity != null && c.PrimaryCity.ToLower().Contains(s)) ||
                (c.PrimaryState != null && c.PrimaryState.ToLower().Contains(s)));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.CustomerName)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CustomerDto(
                c.Id, c.CompanyId, c.Company.CompanyName, c.CustomerCode, c.CustomerName,
                c.PrimaryContactFirstName, c.PrimaryContactLastName, c.PrimaryEmail,
                c.SecondaryEmail, c.PhoneNumber, c.MobileNumber,
                c.PrimaryAddress, c.PrimaryCity, c.PrimaryState, c.PrimaryPostalCode,
                c.SecondaryAddress, c.SecondaryCity, c.SecondaryState, c.SecondaryPostalCode,
                c.IsActive, c.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<CustomerDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
