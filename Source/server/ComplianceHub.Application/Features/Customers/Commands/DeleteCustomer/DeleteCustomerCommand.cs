using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Domain.Enums;
using MediatR;

namespace ComplianceHub.Application.Features.Customers.Commands.DeleteCustomer;

public record DeleteCustomerCommand(Guid Id, Guid CompanyId) : IRequest;

public class DeleteCustomerCommandHandler(IUnitOfWork uow) : IRequestHandler<DeleteCustomerCommand>
{
    public async Task Handle(DeleteCustomerCommand request, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        if (customer.CompanyId != request.CompanyId)
            throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        customer.IsDeleted = true;
        customer.IsActive = false;
        customer.UpdatedAt = DateTime.UtcNow;
        uow.Customers.Update(customer);

        var secUser = await uow.SecurityUsers.FindFirstAsync(
            u => u.UserId == customer.Id && u.Role == UserRole.Customer && !u.IsDeleted, ct);
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
