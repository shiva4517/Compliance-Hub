using FluentValidation;

namespace ComplianceHub.Application.Features.Districts.Commands.CreateDistrict;

public class CreateDistrictCommandValidator : AbstractValidator<CreateDistrictCommand>
{
    public CreateDistrictCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty().WithMessage("Company is required.");
        RuleFor(x => x.Name).NotEmpty().WithMessage("District name is required.").MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => !string.IsNullOrEmpty(x.Description));
    }
}
