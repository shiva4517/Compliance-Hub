using FluentValidation;

namespace ComplianceHub.Application.Features.AiPrAutomation.Commands.StartAiPrAutomation;

public sealed class StartAiPrAutomationCommandValidator : AbstractValidator<StartAiPrAutomationCommand>
{
    public StartAiPrAutomationCommandValidator()
    {
        RuleFor(x => x.Request.RepositoryUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.Request.RepositoryOwner).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.RepositoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.BaseBranch).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.TaskTitle).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.TaskDescription).NotEmpty().MaximumLength(8000);
        RuleFor(x => x.Request.MaxIterations)
            .InclusiveBetween(1, 10)
            .When(x => x.Request.MaxIterations.HasValue);
        RuleForEach(x => x.Request.ValidationCommands)
            .NotEmpty()
            .MaximumLength(500);
    }
}
