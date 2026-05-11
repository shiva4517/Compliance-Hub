using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Companies.Commands.UpdateCompany;

public record UpdateCompanyCommand(
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
    string? WebsiteUrl,
    bool IsActive
) : IRequest;

public class UpdateCompanyCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCompanyCommand>
{
    public async Task Handle(UpdateCompanyCommand request, CancellationToken ct)
    {
        var company = await uow.Companies.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Company), request.Id);

        var emailTaken = await uow.Companies.ExistsAsync(
            c => c.Id != request.Id && c.PrimaryEmail.ToLower() == request.PrimaryEmail.ToLower(), ct);
        if (emailTaken)
            throw new ConflictException($"A company with email '{request.PrimaryEmail}' already exists.");

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
        company.IsActive = request.IsActive;
        company.UpdatedAt = DateTime.UtcNow;
        company.UpdatedBy = currentUser.Email;

        uow.Companies.Update(company);

        // Sync Admin-role SecurityUsers linked to this company
        var secUsers = await uow.SecurityUsers.FindAsync(
            u => u.UserId == company.Id && u.Role == UserRole.Admin && !u.IsDeleted, ct);
        foreach (var su in secUsers)
        {
            su.IsActive = request.IsActive;
            su.UpdatedAt = DateTime.UtcNow;
            su.UpdatedBy = currentUser.Email;
            uow.SecurityUsers.Update(su);
        }

        await uow.SaveChangesAsync(ct);
    }
}
