using FluentValidation;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.UpdateCompanyDivision;

public class UpdateCompanyDivisionCommandValidator : AbstractValidator<UpdateCompanyDivisionCommand>
{
    public UpdateCompanyDivisionCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().WithMessage("Division name is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
    }
}
