using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Departments.Commands.ToggleDepartmentStatus;

public record ToggleDepartmentStatusCommand(Guid Id, bool IsActive) : IRequest;

public class ToggleDepartmentStatusCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<ToggleDepartmentStatusCommand>
{
    public async Task Handle(ToggleDepartmentStatusCommand request, CancellationToken ct)
    {
        var department = await uow.Departments.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Department), request.Id);

        department.IsActive = request.IsActive;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = currentUser.Email;

        uow.Departments.Update(department);
        await uow.SaveChangesAsync(ct);
    }
}
