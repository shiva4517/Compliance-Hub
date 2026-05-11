using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Companies.Queries.GetCompanies;

public record GetCompaniesQuery(string? Search, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<CompanyDto>>;

public record CompanyDto(
    Guid Id,
    string CompanyCode,
    string CompanyName,
    string PrimaryEmail,
    string? SecondaryEmail,
    string? PhoneNumber,
    string PrimaryAddress,
    string PrimaryCity,
    string PrimaryState,
    string PrimaryPostalCode,
    string? SecondaryAddress,
    string? SecondaryCity,
    string? SecondaryState,
    string? SecondaryPostalCode,
    string? WebsiteUrl,
    bool IsActive,
    DateTime CreatedAt
);

public class GetCompaniesQueryHandler(IUnitOfWork uow) : IRequestHandler<GetCompaniesQuery, PaginatedList<CompanyDto>>
{
    public async Task<PaginatedList<CompanyDto>> Handle(GetCompaniesQuery request, CancellationToken ct)
    {
        var query = uow.Companies.Query();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(c =>
                c.CompanyCode.ToLower().Contains(s) ||
                c.CompanyName.ToLower().Contains(s) ||
                c.PrimaryEmail.ToLower().Contains(s) ||
                (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(s)) ||
                c.PrimaryCity.ToLower().Contains(s) ||
                c.PrimaryState.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(c => c.CompanyCode)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CompanyDto(
                c.Id, c.CompanyCode, c.CompanyName, c.PrimaryEmail, c.SecondaryEmail,
                c.PhoneNumber, c.PrimaryAddress, c.PrimaryCity, c.PrimaryState, c.PrimaryPostalCode,
                c.SecondaryAddress, c.SecondaryCity, c.SecondaryState, c.SecondaryPostalCode,
                c.WebsiteUrl, c.IsActive, c.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<CompanyDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
