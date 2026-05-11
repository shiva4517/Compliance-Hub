using FluentValidation;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.CreateCompanyDivision;

public class CreateCompanyDivisionCommandValidator : AbstractValidator<CreateCompanyDivisionCommand>
{
    public CreateCompanyDivisionCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Company is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Division name is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
    }
}
