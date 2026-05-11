using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.ToggleCompanyDivisionStatus;

public record ToggleCompanyDivisionStatusCommand(Guid Id, bool IsActive) : IRequest;

public class ToggleCompanyDivisionStatusCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<ToggleCompanyDivisionStatusCommand>
{
    public async Task Handle(ToggleCompanyDivisionStatusCommand request, CancellationToken ct)
    {
        var division = await uow.CompanyDivisions.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyDivision), request.Id);

        division.IsActive = request.IsActive;
        division.UpdatedAt = DateTime.UtcNow;
        division.UpdatedBy = currentUser.Email;

        uow.CompanyDivisions.Update(division);
        await uow.SaveChangesAsync(ct);
    }
}
