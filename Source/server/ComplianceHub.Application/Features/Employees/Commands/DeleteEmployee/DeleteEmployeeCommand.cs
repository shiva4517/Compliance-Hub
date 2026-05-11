using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Employees.Commands.DeleteEmployee;

public record DeleteEmployeeCommand(Guid Id) : IRequest;

public class DeleteEmployeeCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteEmployeeCommand>
{
    public async Task Handle(DeleteEmployeeCommand request, CancellationToken ct)
    {
        var employee = await uow.Employees.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Employee), request.Id);

        employee.IsDeleted = true;
        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;
        uow.Employees.Update(employee);

        // Soft-delete linked SecurityUser (identified by Role=Employee + UserId)
        var secUser = await uow.SecurityUsers.FindFirstAsync(
            u => u.UserId == employee.Id && u.Role == UserRole.Employee && !u.IsDeleted, ct);
        if (secUser is not null)
        {
            secUser.IsDeleted = true;
            secUser.IsActive = false;
            secUser.UpdatedAt = DateTime.UtcNow;
            uow.SecurityUsers.Update(secUser);
        }

        await uow.SaveChangesAsync(ct);
    }
}
