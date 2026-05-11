using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Companies.Commands.RestoreCompany;

public record RestoreCompanyCommand(
    Guid Id,
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
    string? WebsiteUrl
) : IRequest<string>;

public class RestoreCompanyCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<RestoreCompanyCommand, string>
{
    public async Task<string> Handle(RestoreCompanyCommand request, CancellationToken ct)
    {
        var company = await uow.Companies.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Company), request.Id);

        company.CompanyName = request.CompanyName;
        company.PrimaryEmail = request.PrimaryEmail;
        company.SecondaryEmail = request.SecondaryEmail;
        company.PhoneNumber = request.PhoneNumber;
        company.PrimaryAddress = request.PrimaryAddress;
        company.PrimaryCity = request.PrimaryCity;
        company.PrimaryState = request.PrimaryState;
        company.PrimaryPostalCode = request.PrimaryPostalCode;
        company.SecondaryAddress = request.SecondaryAddress;
        company.SecondaryCity = request.SecondaryCity;
        company.SecondaryState = request.SecondaryState;
        company.SecondaryPostalCode = request.SecondaryPostalCode;
        company.WebsiteUrl = request.WebsiteUrl;
        company.IsActive = true;
        company.IsDeleted = false;
        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = currentUser.Email;
        uow.Companies.Update(company);

        // Restore linked Admin SecurityUser
        var adminUser = await uow.SecurityUsers.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.UserId == company.Id && u.Role == UserRole.Admin, ct);

        var tempPassword = GenerateTempPassword();
        if (adminUser is not null)
        {
            adminUser.Email = request.PrimaryEmail;
            adminUser.PasswordHash = ComputeSha512(tempPassword);
            adminUser.IsActive = true;
            adminUser.IsDeleted = false;
            adminUser.IsForcePasswordChange = true;
            adminUser.UpdatedAt = DateTime.UtcNow;
            adminUser.UpdatedBy = currentUser.Email;
            uow.SecurityUsers.Update(adminUser);
        }

        await uow.SaveChangesAsync(ct);

        try { await emailService.SendWelcomeEmailAsync(request.PrimaryEmail, request.CompanyName, request.PrimaryEmail, tempPassword, ct); }
        catch { }

        return company.CompanyCode;
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
