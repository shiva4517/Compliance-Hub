using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Customers.Commands.UpdateCustomer;

public record UpdateCustomerCommand(
    Guid Id,
    Guid CompanyId,
    string CustomerName,
    string PrimaryContactFirstName,
    string PrimaryContactLastName,
    string PrimaryEmail,
    string? SecondaryEmail,
    string? PhoneNumber,
    string? MobileNumber,
    string? PrimaryAddress,
    string? PrimaryCity,
    string? PrimaryState,
    string? PrimaryPostalCode,
    string? SecondaryAddress,
    string? SecondaryCity,
    string? SecondaryState,
    string? SecondaryPostalCode
) : IRequest;

public class UpdateCustomerCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request, CancellationToken ct)
    {
        var customer = await uow.Customers.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        if (customer.CompanyId != request.CompanyId)
            throw new NotFoundException(nameof(Domain.Entities.Customer), request.Id);

        // If email changed, validate uniqueness within company
        if (!string.Equals(customer.PrimaryEmail, request.PrimaryEmail, StringComparison.OrdinalIgnoreCase))
        {
            var emailTaken = await uow.Customers.Query()
                .AnyAsync(c => c.CompanyId == request.CompanyId &&
                               c.PrimaryEmail.ToLower() == request.PrimaryEmail.ToLower() &&
                               c.Id != request.Id &&
                               !c.IsDeleted, ct);
            if (emailTaken)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["PrimaryEmail"] = [$"A customer with email '{request.PrimaryEmail}' already exists in this company."]
                });

            // Sync SecurityUser email
            var secUser = await uow.SecurityUsers.FindFirstAsync(
                u => u.UserId == customer.Id && u.Role == UserRole.Customer && !u.IsDeleted, ct);
            if (secUser is not null)
            {
                secUser.Email = request.PrimaryEmail;
                secUser.FirstName = request.PrimaryContactFirstName;
                secUser.LastName = request.PrimaryContactLastName;
                secUser.UpdatedAt = DateTime.UtcNow;
                secUser.UpdatedBy = currentUser.Email;
                uow.SecurityUsers.Update(secUser);
            }
        }

        customer.CustomerName = request.CustomerName;
        customer.PrimaryContactFirstName = request.PrimaryContactFirstName;
        customer.PrimaryContactLastName = request.PrimaryContactLastName;
        customer.PrimaryEmail = request.PrimaryEmail;
        customer.SecondaryEmail = request.SecondaryEmail;
        customer.PhoneNumber = request.PhoneNumber;
        customer.MobileNumber = request.MobileNumber;
        customer.PrimaryAddress = request.PrimaryAddress;
        customer.PrimaryCity = request.PrimaryCity;
        customer.PrimaryState = request.PrimaryState;
        customer.PrimaryPostalCode = request.PrimaryPostalCode;
        customer.SecondaryAddress = request.SecondaryAddress;
        customer.SecondaryCity = request.SecondaryCity;
        customer.SecondaryState = request.SecondaryState;
        customer.SecondaryPostalCode = request.SecondaryPostalCode;
        customer.UpdatedAt = DateTime.UtcNow;
        customer.UpdatedBy = currentUser.Email;

        uow.Customers.Update(customer);
        await uow.SaveChangesAsync(ct);
    }
}
