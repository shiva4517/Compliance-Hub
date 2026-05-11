using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Commands.CreateCustomer;

public record CreateCustomerCommand(
    Guid CompanyId,
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
    bool SkipRestoreCheck = false
) : IRequest<Guid>;

public class CreateCustomerCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<CreateCustomerCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request, CancellationToken ct)
    {
        var company = await uow.Companies.GetByIdAsync(request.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);

        var existingCustomer = await uow.Customers.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.CompanyId == request.CompanyId &&
                                      c.PrimaryEmail.ToLower() == request.PrimaryEmail.ToLower(), ct);
        if (existingCustomer is not null)
        {
            if (existingCustomer.IsActive || !existingCustomer.IsDeleted)
                throw new ConflictException($"A customer with email '{request.PrimaryEmail}' already exists.");
            if (!request.SkipRestoreCheck)
                throw new RecordInactiveException(existingCustomer.Id, $"A customer with email '{request.PrimaryEmail}' exists but is inactive.");
        }

        var customerCode = await GenerateCustomerCodeAsync(request.CompanyId, ct);

        var customer = new Customer
        {
            CompanyId = request.CompanyId,
            CustomerCode = customerCode,
            CustomerName = request.CustomerName,
            PrimaryContactFirstName = request.PrimaryContactFirstName,
            PrimaryContactLastName = request.PrimaryContactLastName,
            PrimaryEmail = request.PrimaryEmail,
            SecondaryEmail = request.SecondaryEmail,
            PhoneNumber = request.PhoneNumber,
            MobileNumber = request.MobileNumber,
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

        await uow.Customers.AddAsync(customer, ct);

        var tempPassword = GenerateTempPassword();// GenerateTempPassword();
        var secUser = new SecurityUser
        {
            FirstName = request.PrimaryContactFirstName,
            LastName = request.PrimaryContactLastName,
            Email = request.PrimaryEmail,
            PasswordHash = ComputeSha512(tempPassword),
            Role = UserRole.Customer,
            UserId = customer.Id,
            IsActive = true,
            IsForcePasswordChange = true,
            CreatedBy = currentUser.Email
        };
        await uow.SecurityUsers.AddAsync(secUser, ct);

        await uow.SaveChangesAsync(ct);

        var fullName = $"{request.PrimaryContactFirstName} {request.PrimaryContactLastName}";
        try
        {
            await emailService.SendWelcomeEmailAsync(
                request.PrimaryEmail, fullName, request.PrimaryEmail, tempPassword, ct);
        }
        catch { /* email failure must not roll back the record */ }

        return customer.Id;
    }

    private async Task<string> GenerateCustomerCodeAsync(Guid companyId, CancellationToken ct)
    {
        var lastCode = await uow.Customers.Query()
            .IgnoreQueryFilters()
            .Where(c => c.CompanyId == companyId)
            .OrderByDescending(c => c.CustomerCode)
            .Select(c => c.CustomerCode)
            .FirstOrDefaultAsync(ct);

        if (lastCode is not null && lastCode.StartsWith("CUST-") &&
            int.TryParse(lastCode[5..], out var num))
            return $"CUST-{num + 1:D4}";

        return "CUST-0001";
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
