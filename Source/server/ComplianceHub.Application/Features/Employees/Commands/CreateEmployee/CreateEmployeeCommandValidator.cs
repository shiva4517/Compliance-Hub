using FluentValidation;

namespace ComplianceHub.Application.Features.Employees.Commands.CreateEmployee;

public class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.SecondaryEmail).EmailAddress().MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SecondaryEmail));
        RuleFor(x => x.PhoneNumber).Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.MobileNumber).Matches(@"^\+?[\d\s\-\(\)]{7,20}$")
            .When(x => !string.IsNullOrEmpty(x.MobileNumber));
        RuleFor(x => x.PrimaryAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PrimaryCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryState).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PrimaryPostalCode).NotEmpty().MaximumLength(10);
    }
}
