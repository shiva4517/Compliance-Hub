using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetEmployees;

public record GetEmployeesQuery(string? Search, Guid? CompanyId, int PageNumber = 1, int PageSize = 20)
    : IRequest<PaginatedList<EmployeeDto>>;

public record EmployeeDto(
    Guid Id,
    string EmployeeCode,
    Guid CompanyId,
    string CompanyName,
    string FirstName,
    string LastName,
    string FullName,
    string PrimaryEmail,
    string? SecondaryEmail,
    string? PhoneNumber,
    string? MobileNumber,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? CompanyDivisionId,
    string? DivisionName,
    Guid? CompanyDistrictId,
    string? DistrictName,
    string PrimaryAddress,
    string PrimaryCity,
    string PrimaryState,
    string PrimaryPostalCode,
    string? SecondaryAddress,
    string? SecondaryCity,
    string? SecondaryState,
    string? SecondaryPostalCode,
    bool IsActive,
    DateTime CreatedAt
);

public class GetEmployeesQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetEmployeesQuery, PaginatedList<EmployeeDto>>
{
    public async Task<PaginatedList<EmployeeDto>> Handle(GetEmployeesQuery request, CancellationToken ct)
    {
        IQueryable<Employee> query = uow.Employees.Query()
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.CompanyDivision)
            .Include(e => e.District);

        if (request.CompanyId.HasValue)
            query = query.Where(e => e.CompanyId == request.CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.ToLower();
            query = query.Where(e =>
                e.EmployeeCode.ToLower().Contains(s) ||
                e.FirstName.ToLower().Contains(s) ||
                e.LastName.ToLower().Contains(s) ||
                e.PrimaryEmail.ToLower().Contains(s) ||
                (e.PhoneNumber != null && e.PhoneNumber.ToLower().Contains(s)) ||
                e.PrimaryCity.ToLower().Contains(s) ||
                e.PrimaryState.ToLower().Contains(s) ||
                e.Company.CompanyName.ToLower().Contains(s));
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(e => e.EmployeeCode)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EmployeeDto(
                e.Id,
                e.EmployeeCode,
                e.CompanyId,
                e.Company.CompanyName,
                e.FirstName,
                e.LastName,
                e.FirstName + " " + e.LastName,
                e.PrimaryEmail,
                e.SecondaryEmail,
                e.PhoneNumber,
                e.MobileNumber,
                e.DepartmentId,
                e.Department != null ? e.Department.Name : null,
                e.CompanyDivisionId,
                e.CompanyDivision != null ? e.CompanyDivision.Name : null,
                e.CompanyDistrictId,
                e.District != null ? e.District.Name : null,
                e.PrimaryAddress,
                e.PrimaryCity,
                e.PrimaryState,
                e.PrimaryPostalCode,
                e.SecondaryAddress,
                e.SecondaryCity,
                e.SecondaryState,
                e.SecondaryPostalCode,
                e.IsActive,
                e.CreatedAt))
            .ToListAsync(ct);

        return new PaginatedList<EmployeeDto>
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}
