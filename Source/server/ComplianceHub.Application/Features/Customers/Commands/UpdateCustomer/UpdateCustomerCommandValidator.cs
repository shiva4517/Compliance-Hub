using FluentValidation;

namespace ComplianceHub.Application.Features.Customers.Commands.UpdateCustomer;

public class UpdateCustomerCommandValidator : AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryContactFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryContactLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.SecondaryEmail).EmailAddress().MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SecondaryEmail));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.MobileNumber).MaximumLength(20);
    }
}
