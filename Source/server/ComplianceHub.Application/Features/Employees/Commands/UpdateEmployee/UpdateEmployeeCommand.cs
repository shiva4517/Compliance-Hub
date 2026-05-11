using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Employees.Commands.UpdateEmployee;

public record UpdateEmployeeCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? SecondaryEmail,
    string? PhoneNumber,
    string? MobileNumber,
    Guid? DepartmentId,
    Guid? CompanyDivisionId,
    Guid? CompanyDistrictId,
    string PrimaryAddress,
    string PrimaryCity,
    string PrimaryState,
    string PrimaryPostalCode,
    string? SecondaryAddress,
    string? SecondaryCity,
    string? SecondaryState,
    string? SecondaryPostalCode,
    bool IsActive
) : IRequest;

public class UpdateEmployeeCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateEmployeeCommand>
{
    public async Task Handle(UpdateEmployeeCommand request, CancellationToken ct)
    {
        var employee = await uow.Employees.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Employee), request.Id);

        if (request.CompanyDivisionId.HasValue)
        {
            var divisionValid = await uow.CompanyDivisions.ExistsAsync(
                d => d.Id == request.CompanyDivisionId.Value && d.CompanyId == employee.CompanyId, ct);
            if (!divisionValid)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["CompanyDivisionId"] = ["CompanyDivisionId does not belong to this employee's company."]
                });
        }

        employee.FirstName = request.FirstName;
        employee.LastName = request.LastName;
        employee.SecondaryEmail = request.SecondaryEmail;
        employee.PhoneNumber = request.PhoneNumber;
        employee.MobileNumber = request.MobileNumber;
        employee.DepartmentId = request.DepartmentId;
        employee.CompanyDivisionId = request.CompanyDivisionId;
        employee.CompanyDistrictId = request.CompanyDistrictId;
        employee.PrimaryAddress = request.PrimaryAddress;
        employee.PrimaryCity = request.PrimaryCity;
        employee.PrimaryState = request.PrimaryState;
        employee.PrimaryPostalCode = request.PrimaryPostalCode;
        employee.SecondaryAddress = request.SecondaryAddress;
        employee.SecondaryCity = request.SecondaryCity;
        employee.SecondaryState = request.SecondaryState;
        employee.SecondaryPostalCode = request.SecondaryPostalCode;
        employee.IsActive = request.IsActive;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = currentUser.Email;

        uow.Employees.Update(employee);

        // Sync linked SecurityUser (identified by Role=Employee + UserId)
        var secUser = await uow.SecurityUsers.FindFirstAsync(
            u => u.UserId == employee.Id && u.Role == UserRole.Employee && !u.IsDeleted, ct);
        if (secUser is not null)
        {
            secUser.FirstName = request.FirstName;
            secUser.LastName = request.LastName;
            secUser.PhoneNumber = request.PhoneNumber;
            secUser.IsActive = request.IsActive;
            secUser.UpdatedAt = DateTime.UtcNow;
            secUser.UpdatedBy = currentUser.Email;
            uow.SecurityUsers.Update(secUser);
        }

        await uow.SaveChangesAsync(ct);
    }
}
