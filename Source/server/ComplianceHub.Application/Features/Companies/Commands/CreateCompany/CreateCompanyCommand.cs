using System.Security.Cryptography;
using System.Text;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Companies.Commands.CreateCompany;

public record CreateCompanyCommand(
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
    bool SkipRestoreCheck = false
) : IRequest<CreateCompanyResponse>;

public record CreateCompanyResponse(Guid CompanyId, string CompanyCode);

public class CreateCompanyCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser, IEmailService emailService)
    : IRequestHandler<CreateCompanyCommand, CreateCompanyResponse>
{
    public async Task<CreateCompanyResponse> Handle(CreateCompanyCommand request, CancellationToken ct)
    {
        var existingCompany = await uow.Companies.Query()
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.PrimaryEmail.ToLower() == request.PrimaryEmail.ToLower(), ct);

        if (existingCompany is not null)
        {
            if (existingCompany.IsActive || !existingCompany.IsDeleted)
                throw new ConflictException($"A company with email '{request.PrimaryEmail}' already exists.");
            if (!request.SkipRestoreCheck)
                throw new RecordInactiveException(existingCompany.Id, $"A company with email '{request.PrimaryEmail}' exists but is inactive.");
        }

        var companyCode = await GenerateCompanyCodeAsync(ct);

        var company = new Company
        {
            CompanyCode = companyCode,
            CompanyName = request.CompanyName,
            PrimaryEmail = request.PrimaryEmail,
            SecondaryEmail = request.SecondaryEmail,
            PhoneNumber = request.PhoneNumber,
            PrimaryAddress = request.PrimaryAddress,
            PrimaryCity = request.PrimaryCity,
            PrimaryState = request.PrimaryState,
            PrimaryPostalCode = request.PrimaryPostalCode,
            SecondaryAddress = request.SecondaryAddress,
            SecondaryCity = request.SecondaryCity,
            SecondaryState = request.SecondaryState,
            SecondaryPostalCode = request.SecondaryPostalCode,
            WebsiteUrl = request.WebsiteUrl,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.Companies.AddAsync(company, ct);
        await uow.SaveChangesAsync(ct);

        // Create initial Admin SecurityUser for the company
        var adminGroup = await uow.SecurityGroups.FindFirstAsync(g => g.Role == UserRole.Admin && !g.IsDeleted, ct);
        var tempPassword = GenerateTempPassword();// GenerateTempPassword();
        var adminUser = new SecurityUser
        {
            FirstName = request.CompanyName,
            LastName = "Admin",
            Email = request.PrimaryEmail,
            PasswordHash = ComputeSha512(tempPassword),
            Role = UserRole.Admin,
            SecurityGroupId = adminGroup?.Id,
            UserId = company.Id,
            IsActive = true,
            IsForcePasswordChange = true,
            CreatedBy = currentUser.Email
        };
        await uow.SecurityUsers.AddAsync(adminUser, ct);
        await uow.SaveChangesAsync(ct);

        try { await emailService.SendWelcomeEmailAsync(request.PrimaryEmail, request.CompanyName, request.PrimaryEmail, tempPassword, ct); }
        catch { }

        return new CreateCompanyResponse(company.Id, company.CompanyCode);
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

    private async Task<string> GenerateCompanyCodeAsync(CancellationToken ct)
    {
        var lastCode = await uow.Companies.Query()
            .OrderByDescending(c => c.CompanyCode)
            .Select(c => c.CompanyCode)
            .FirstOrDefaultAsync(ct);

        int nextNumber = 1;
        if (lastCode is not null && lastCode.StartsWith("COMP-") &&
            int.TryParse(lastCode[5..], out var parsed))
        {
            nextNumber = parsed + 1;
        }

        return $"COMP-{nextNumber:D4}";
    }
}
