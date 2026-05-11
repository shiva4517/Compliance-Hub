using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Commands.RestoreCustomer;

public record RestoreCustomerCommand(
    Guid Id,
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
    string? SecondaryPostalCode
) : IRequest;

public class RestoreCustomerCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<RestoreCustomerCommand>
{
    public async Task Handle(RestoreCustomerCommand request, CancellationToken ct)
    {
        var customer = await uow.Customers.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.Id && c.CompanyId == request.CompanyId, ct)
            ?? throw new NotFoundException(nameof(Customer), request.Id);

        customer.CustomerName = request.CustomerName;
        customer.PrimaryContactFirstName = request.PrimaryContactFirstName;
        customer.PrimaryContactLastName = request.PrimaryContactLastName;
        customer.PrimaryEmail = request.PrimaryEmail;
        customer.SecondaryEmail = request.SecondaryEmail;
        customer.PhoneNumber = request.PhoneNumber;
        customer.MobileNumber = request.MobileNumber;
        customer.PrimaryAddress = request.PrimaryAddress;
        customer.PrimaryCity = request.PrimaryCity;
        customer.PrimaryState = request.PrimaryState;
        customer.PrimaryPostalCode = request.PrimaryPostalCode;
        customer.SecondaryAddress = request.SecondaryAddress;
        customer.SecondaryCity = request.SecondaryCity;
        customer.SecondaryState = request.SecondaryState;
        customer.SecondaryPostalCode = request.SecondaryPostalCode;
        customer.IsActive = true;
        customer.IsDeleted = false;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = currentUser.Email;
        uow.Customers.Update(customer);

        var secUser = await uow.SecurityUsers.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == customer.Id && u.Role == UserRole.Customer, ct);

        var tempPassword = GenerateTempPassword();
        if (secUser is not null)
        {
            secUser.FirstName = request.PrimaryContactFirstName;
            secUser.LastName = request.PrimaryContactLastName;
            secUser.Email = request.PrimaryEmail;
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
            await uow.SecurityUsers.AddAsync(new SecurityUser
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
            }, ct);
        }

        await uow.SaveChangesAsync(ct);

        var fullName = $"{request.PrimaryContactFirstName} {request.PrimaryContactLastName}";
        try { await emailService.SendWelcomeEmailAsync(request.PrimaryEmail, fullName, request.PrimaryEmail, tempPassword, ct); }
        catch { }
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
