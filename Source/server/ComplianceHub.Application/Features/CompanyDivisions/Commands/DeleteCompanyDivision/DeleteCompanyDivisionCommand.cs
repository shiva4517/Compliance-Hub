using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.DeleteCompanyDivision;

public record DeleteCompanyDivisionCommand(Guid Id) : IRequest;

public class DeleteCompanyDivisionCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCompanyDivisionCommand>
{
    public async Task Handle(DeleteCompanyDivisionCommand request, CancellationToken ct)
    {
        var division = await uow.CompanyDivisions.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyDivision), request.Id);

        division.IsDeleted = true;
        division.UpdatedAt = DateTime.UtcNow;
        uow.CompanyDivisions.Update(division);
        await uow.SaveChangesAsync(ct);
    }
}
