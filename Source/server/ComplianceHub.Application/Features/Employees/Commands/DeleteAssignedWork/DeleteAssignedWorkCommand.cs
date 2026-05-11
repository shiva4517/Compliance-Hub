using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Commands.DeleteAssignedWork;

public record DeleteAssignedWorkCommand(Guid Id) : IRequest;

public class DeleteAssignedWorkCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<DeleteAssignedWorkCommand>
{
    public async Task Handle(DeleteAssignedWorkCommand request, CancellationToken ct)
    {
        var assignment = await uow.EmployeeAssignedWorks.Query()
            .FirstOrDefaultAsync(a => a.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EmployeeAssignedWork), request.Id);

        var now = DateTime.UtcNow;
        assignment.IsActive = false;
        assignment.IsDeleted = true;
        assignment.UpdatedAt = now;
        assignment.UpdatedBy = currentUser.Email ?? "system";

        await uow.SaveChangesAsync(ct);
    }
}
