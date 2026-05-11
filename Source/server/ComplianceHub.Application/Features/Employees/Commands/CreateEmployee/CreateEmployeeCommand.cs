using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Commands.CreateEmployee;

public record CreateEmployeeCommand(
    Guid CompanyId,
    string FirstName,
    string LastName,
    string PrimaryEmail,
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
    bool SkipRestoreCheck = false
) : IRequest<CreateEmployeeResponse>;

public record CreateEmployeeResponse(Guid EmployeeId, string EmployeeCode);

public class CreateEmployeeCommandHandler(
    IUnitOfWork uow,
    ICurrentUserService currentUser,
    IEmailService emailService)
    : IRequestHandler<CreateEmployeeCommand, CreateEmployeeResponse>
{
    public async Task<CreateEmployeeResponse> Handle(CreateEmployeeCommand request, CancellationToken ct)
    {
        var company = await uow.Companies.GetByIdAsync(request.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        var existingEmployee = await uow.Employees.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(e => e.CompanyId == request.CompanyId
                && e.PrimaryEmail.ToLower() == request.PrimaryEmail.ToLower(), ct);

        if (existingEmployee is not null)
        {
            if (existingEmployee.IsActive || !existingEmployee.IsDeleted)
                throw new ConflictException($"An employee with email '{request.PrimaryEmail}' already exists in this company.");
            if (!request.SkipRestoreCheck)
                throw new RecordInactiveException(existingEmployee.Id, $"An employee with email '{request.PrimaryEmail}' exists but is inactive.");
        }

        if (request.CompanyDivisionId.HasValue)
        {
            var divisionValid = await uow.CompanyDivisions.ExistsAsync(
                d => d.Id == request.CompanyDivisionId.Value && d.CompanyId == request.CompanyId, ct);
            if (!divisionValid)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["CompanyDivisionId"] = ["CompanyDivisionId does not belong to the specified company."]
                });
        }

        var employeeCode = await GenerateEmployeeCodeAsync(request.CompanyId, ct);

        var employee = new Employee
        {
            CompanyId = request.CompanyId,
            EmployeeCode = employeeCode,
            FirstName = request.FirstName,
            LastName = request.LastName,
            PrimaryEmail = request.PrimaryEmail,
            SecondaryEmail = request.SecondaryEmail,
            PhoneNumber = request.PhoneNumber,
            MobileNumber = request.MobileNumber,
            DepartmentId = request.DepartmentId,
            CompanyDivisionId = request.CompanyDivisionId,
            CompanyDistrictId = request.CompanyDistrictId,
            PrimaryAddress = request.PrimaryAddress,
            PrimaryCity = request.PrimaryCity,
            PrimaryState = request.PrimaryState,
            PrimaryPostalCode = request.PrimaryPostalCode,
            SecondaryAddress = request.SecondaryAddress,
            SecondaryCity = request.SecondaryCity,
            SecondaryState = request.SecondaryState,
            SecondaryPostalCode = request.SecondaryPostalCode,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.Employees.AddAsync(employee, ct);

        var tempPassword = GenerateTempPassword();

        var groupId = await uow.SecurityGroups.Query().FirstOrDefaultAsync(x => x.GroupName.ToLower() == "employees");
        var securityUser = new SecurityUser
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.PrimaryEmail,
            PasswordHash = ComputeSha512(tempPassword),
            PhoneNumber = request.PhoneNumber,
            SecurityGroupId = groupId?.Id,
            Role = UserRole.Employee,
            UserId = employee.Id,
            IsActive = true,
            IsForcePasswordChange = true,
            CreatedBy = currentUser.Email
        };

        await uow.SecurityUsers.AddAsync(securityUser, ct);
        await uow.SaveChangesAsync(ct);

        _ = emailService.SendEmployeeWelcomeEmailAsync(
            request.PrimaryEmail,
            employee.FullName,
            company.CompanyName,
            request.PrimaryEmail,
            tempPassword,
            CancellationToken.None);

        return new CreateEmployeeResponse(employee.Id, employee.EmployeeCode);
    }

    private async Task<string> GenerateEmployeeCodeAsync(Guid companyId, CancellationToken ct)
    {
        var lastCode = await uow.Employees.Query()
            .Where(e => e.CompanyId == companyId)
            .OrderByDescending(e => e.EmployeeCode)
            .Select(e => e.EmployeeCode)
            .FirstOrDefaultAsync(ct);

        int nextNumber = 1;
        if (lastCode is not null && lastCode.StartsWith("EMP-") &&
            int.TryParse(lastCode[4..], out var parsed))
        {
            nextNumber = parsed + 1;
        }

        return $"EMP-{nextNumber:D4}";
    }

    private static string GenerateTempPassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghjkmnpqrstuvwxyz";
        const string digits = "23456789";
        const string special = "!@#$%";
        const string all = upper + lower + digits + special;

        var bytes = new byte[16];
        RandomNumberGenerator.Fill(bytes);

        var chars = new char[12];
        chars[0] = upper[bytes[0] % upper.Length];
        chars[1] = lower[bytes[1] % lower.Length];
        chars[2] = digits[bytes[2] % digits.Length];
        chars[3] = special[bytes[3] % special.Length];
        for (var i = 4; i < 12; i++)
            chars[i] = all[bytes[i] % all.Length];

        return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(100)).ToArray());
    }

    private static string ComputeSha512(string input)
    {
        var bytes = SHA512.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLower();
    }
}
