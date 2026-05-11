using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetMyProfile;

public record GetMyProfileQuery(Guid EmployeeId) : IRequest<MyProfileDto>;

public record MyProfileDto(
    // Employee
    Guid Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string PrimaryEmail,
    string? SecondaryEmail,
    string? PhoneNumber,
    string? MobileNumber,
    string? DepartmentName,
    string? DivisionName,
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
    DateTime CreatedAt,
    // Company
    Guid CompanyId,
    string CompanyName,
    string CompanyEmail,
    string? CompanyPhone,
    string CompanyAddress,
    string CompanyCity,
    string CompanyState,
    string CompanyPostalCode,
    string? CompanyWebsite);

public class GetMyProfileQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetMyProfileQuery, MyProfileDto>
{
    public async Task<MyProfileDto> Handle(GetMyProfileQuery request, CancellationToken ct)
    {
        var e = await uow.Employees.Query()
            .Include(x => x.Company)
            .Include(x => x.Department)
            .Include(x => x.CompanyDivision)
            .Include(x => x.District)
            .FirstOrDefaultAsync(x => x.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Employee), request.EmployeeId);

        return new MyProfileDto(
            e.Id, e.EmployeeCode, e.FirstName, e.LastName, e.FullName,
            e.PrimaryEmail, e.SecondaryEmail, e.PhoneNumber, e.MobileNumber,
            e.Department?.Name, e.CompanyDivision?.Name, e.District?.Name,
            e.PrimaryAddress, e.PrimaryCity, e.PrimaryState, e.PrimaryPostalCode,
            e.SecondaryAddress, e.SecondaryCity, e.SecondaryState, e.SecondaryPostalCode,
            e.IsActive, e.CreatedAt,
            e.CompanyId, e.Company.CompanyName, e.Company.PrimaryEmail,
            e.Company.PhoneNumber,
            e.Company.PrimaryAddress, e.Company.PrimaryCity,
            e.Company.PrimaryState, e.Company.PrimaryPostalCode,
            e.Company.WebsiteUrl);
    }
}
