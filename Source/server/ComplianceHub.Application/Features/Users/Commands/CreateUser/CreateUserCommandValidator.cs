using FluentValidation;

namespace ComplianceHub.Application.Features.Users.Commands.CreateUser;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        //RuleFor(x => x.Role).IsInEnum();

        //When(x => x.Password != null, () =>
        //{
        //    RuleFor(x => x.Password).MinimumLength(8)
        //        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
        //        .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
        //        .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        //});
    }
}
