using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Customers.Commands.ToggleCustomerStatus;

public record ToggleCustomerStatusCommand(Guid Id, Guid CompanyId, bool IsActive) : IRequest;

public class ToggleCustomerStatusCommandHandler(IUnitOfWork uow) : IRequestHandler<ToggleCustomerStatusCommand>
{
    public async Task Handle(ToggleCustomerStatusCommand request, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        if (customer.CompanyId != request.CompanyId)
            throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        customer.IsActive = request.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;
        uow.Customers.Update(customer);

        var secUser = await uow.SecurityUsers.FindFirstAsync(
            u => u.UserId == customer.Id && u.Role == UserRole.Customer && !u.IsDeleted, ct);
        if (secUser is not null)
        {
            secUser.IsActive = request.IsActive;
            secUser.UpdatedAt = DateTime.UtcNow;
            uow.SecurityUsers.Update(secUser);
        }

        await uow.SaveChangesAsync(ct);
    }
}
