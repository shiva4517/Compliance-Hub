using FluentValidation;

namespace ComplianceHub.Application.Features.AiPrAutomation.Commands.HandleAiPrAutomationEvent;

public sealed class HandleAiPrAutomationEventCommandValidator : AbstractValidator<HandleAiPrAutomationEventCommand>
{
    public HandleAiPrAutomationEventCommandValidator()
    {
        RuleFor(x => x.AutomationEvent.RepositoryUrl).NotEmpty().MaximumLength(1000);
        RuleFor(x => x.AutomationEvent.RepositoryOwner).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AutomationEvent.RepositoryName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AutomationEvent.SourceBranch).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AutomationEvent.TargetBranch).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AutomationEvent.PullRequestNumber).GreaterThan(0);
        RuleFor(x => x.AutomationEvent.PullRequestTitle).NotEmpty().MaximumLength(300);
        RuleForEach(x => x.AutomationEvent.ValidationCommands).NotEmpty().MaximumLength(500);
    }
}
