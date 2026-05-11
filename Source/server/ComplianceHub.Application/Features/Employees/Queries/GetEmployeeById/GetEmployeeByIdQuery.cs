using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetEmployeeById;

public record GetEmployeeByIdQuery(Guid Id) : IRequest<EmployeeDto>;

public class GetEmployeeByIdQueryHandler(IUnitOfWork uow) : IRequestHandler<GetEmployeeByIdQuery, EmployeeDto>
{
    public async Task<EmployeeDto> Handle(GetEmployeeByIdQuery request, CancellationToken ct)
    {
        var e = await uow.Employees.Query()
            .Include(e => e.Company)
            .Include(e => e.Department)
            .Include(e => e.CompanyDivision)
            .Include(e => e.District)
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Employee), request.Id);

        return new EmployeeDto(
            e.Id, e.EmployeeCode, e.CompanyId, e.Company.CompanyName,
            e.FirstName, e.LastName, e.FirstName + " " + e.LastName,
            e.PrimaryEmail, e.SecondaryEmail, e.PhoneNumber, e.MobileNumber,
            e.DepartmentId, e.Department?.Name,
            e.CompanyDivisionId, e.CompanyDivision?.Name,
            e.CompanyDistrictId, e.District?.Name,
            e.PrimaryAddress, e.PrimaryCity, e.PrimaryState, e.PrimaryPostalCode,
            e.SecondaryAddress, e.SecondaryCity, e.SecondaryState, e.SecondaryPostalCode,
            e.IsActive, e.CreatedAt);
    }
}
