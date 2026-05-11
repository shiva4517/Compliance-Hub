using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Companies.Commands.DeleteCompany;

public record DeleteCompanyCommand(Guid Id) : IRequest;

public class DeleteCompanyCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCompanyCommand>
{
    public async Task Handle(DeleteCompanyCommand request, CancellationToken ct)
    {
        var company = await uow.Companies.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Company), request.Id);

        company.IsDeleted = true;
        company.IsActive = false;
        company.UpdatedAt = DateTime.UtcNow;
        uow.Companies.Update(company);

        // Soft-delete Admin-role SecurityUsers linked to this company
        var secUsers = await uow.SecurityUsers.FindAsync(
            u => u.UserId == company.Id && u.Role == UserRole.Admin && !u.IsDeleted, ct);
        foreach (var su in secUsers)
        {
            su.IsDeleted = true;
            su.IsActive = false;
            su.UpdatedAt = DateTime.UtcNow;
            uow.SecurityUsers.Update(su);
        }

        await uow.SaveChangesAsync(ct);
    }
}
