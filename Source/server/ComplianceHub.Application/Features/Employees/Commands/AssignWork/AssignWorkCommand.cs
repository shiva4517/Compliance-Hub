using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Commands.AssignWork;

public record AssignWorkCommand(
    Guid EmployeeId,
    Guid SubscriptionId,
    Guid CustomerId) : IRequest<Guid>;

public class AssignWorkCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<AssignWorkCommand, Guid>
{
    public async Task<Guid> Handle(AssignWorkCommand request, CancellationToken ct)
    {
        var employee = await uow.Employees.Query()
            .FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Employee), request.EmployeeId);

        var subscription = await uow.Subscriptions.Query()
            .FirstOrDefaultAsync(s => s.Id == request.SubscriptionId && s.IsActive, ct)
            ?? throw new NotFoundException(nameof(Subscription), request.SubscriptionId);

        var duplicate = await uow.EmployeeAssignedWorks.Query()
            .AnyAsync(a => a.EmployeeId == request.EmployeeId
                        && a.SubscriptionId == request.SubscriptionId, ct);

        if (duplicate)
            throw new InvalidOperationException("This subscription is already assigned to the employee.");

        var now = DateTime.UtcNow;
        var email = currentUser.Email ?? "system";

        var assignment = new EmployeeAssignedWork
        {
            EmployeeId = request.EmployeeId,
            SubscriptionId = request.SubscriptionId,
            CustomerId = request.CustomerId,
            IsActive = true,
            AssignedAt = now,
            AssignedBy = email,
            CreatedBy = email,
        };

        await uow.EmployeeAssignedWorks.AddAsync(assignment, ct);
        await uow.SaveChangesAsync(ct);
        return assignment.Id;
    }
}
