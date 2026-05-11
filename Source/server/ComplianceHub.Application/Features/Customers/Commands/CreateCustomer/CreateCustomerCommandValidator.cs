using FluentValidation;

namespace ComplianceHub.Application.Features.Customers.Commands.CreateCustomer;

public class CreateCustomerCommandValidator : AbstractValidator<CreateCustomerCommand>
{
    public CreateCustomerCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryContactFirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryContactLastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.SecondaryEmail).EmailAddress().MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SecondaryEmail));
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
        RuleFor(x => x.MobileNumber).MaximumLength(20);
        RuleFor(x => x.PrimaryAddress).MaximumLength(300);
        RuleFor(x => x.PrimaryCity).MaximumLength(100);
        RuleFor(x => x.PrimaryState).MaximumLength(50);
        RuleFor(x => x.PrimaryPostalCode).MaximumLength(10);
        RuleFor(x => x.SecondaryAddress).MaximumLength(300);
        RuleFor(x => x.SecondaryCity).MaximumLength(100);
        RuleFor(x => x.SecondaryState).MaximumLength(50);
        RuleFor(x => x.SecondaryPostalCode).MaximumLength(10);
    }
}
