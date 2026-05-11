using FluentValidation;

namespace ComplianceHub.Application.Features.Companies.Commands.UpdateCompany;

public class UpdateCompanyCommandValidator : AbstractValidator<UpdateCompanyCommand>
{
    public UpdateCompanyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PrimaryEmail).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.SecondaryEmail).EmailAddress().MaximumLength(200).When(x => !string.IsNullOrEmpty(x.SecondaryEmail));
        RuleFor(x => x.PhoneNumber).Matches(@"^\+?[\d\s\-\(\)]{7,20}$").WithMessage("Invalid phone number format.").When(x => !string.IsNullOrEmpty(x.PhoneNumber));
        RuleFor(x => x.PrimaryAddress).NotEmpty().MaximumLength(300);
        RuleFor(x => x.PrimaryCity).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PrimaryState).NotEmpty().MaximumLength(50);
        RuleFor(x => x.PrimaryPostalCode).NotEmpty().MaximumLength(10);
        RuleFor(x => x.WebsiteUrl).Must(BeAValidUrl).WithMessage("Invalid URL format.").When(x => !string.IsNullOrEmpty(x.WebsiteUrl));
    }

    private static bool BeAValidUrl(string? url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var result) &&
        (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
}
