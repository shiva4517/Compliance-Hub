using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Commands.RestoreEmployee;

public record RestoreEmployeeCommand(
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
    string? SecondaryPostalCode
) : IRequest<string>;

public class RestoreEmployeeCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<RestoreEmployeeCommand, string>
{
    public async Task<string> Handle(RestoreEmployeeCommand request, CancellationToken ct)
    {
        var employee = await uow.Employees.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Employee), request.Id);

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
        employee.IsActive = true;
        employee.IsDeleted = false;
        employee.UpdatedAt = DateTime.UtcNow;
        employee.UpdatedBy = currentUser.Email;
        uow.Employees.Update(employee);

        // Restore or recreate SecurityUser
        var secUser = await uow.SecurityUsers.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == employee.Id && u.Role == UserRole.Employee, ct);

        var tempPassword = GenerateTempPassword();
        if (secUser is not null)
        {
            secUser.FirstName = request.FirstName;
            secUser.LastName = request.LastName;
            secUser.PhoneNumber = request.PhoneNumber;
            secUser.PasswordHash = ComputeSha512(tempPassword);
            secUser.IsActive = true;
            secUser.IsDeleted = false;
            secUser.IsForcePasswordChange = true;
            secUser.UpdatedAt = DateTime.UtcNow;
            secUser.UpdatedBy = currentUser.Email;
            uow.SecurityUsers.Update(secUser);
        }
        else
        {
            var newSecUser = new SecurityUser
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = employee.PrimaryEmail,
                PasswordHash = ComputeSha512(tempPassword),
                PhoneNumber = request.PhoneNumber,
                Role = UserRole.Employee,
                UserId = employee.Id,
                IsActive = true,
                IsForcePasswordChange = true,
                CreatedBy = currentUser.Email
            };
            await uow.SecurityUsers.AddAsync(newSecUser, ct);
        }

        await uow.SaveChangesAsync(ct);

        var company = await uow.Companies.GetByIdAsync(employee.CompanyId, ct);
        _ = emailService.SendEmployeeWelcomeEmailAsync(
            employee.PrimaryEmail, employee.FullName,
            company?.CompanyName ?? string.Empty,
            employee.PrimaryEmail, tempPassword, CancellationToken.None);

        return employee.EmployeeCode;
    }

    private static string GenerateTempPassword()
    {
        const string chars = "ABCDEFGHJKMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#";
        return string.Concat(Enumerable.Range(0, 10).Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]));
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
