using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ComplianceHub.Application.Features.Grievances.Commands.CreateGrievance;

public record CreateGrievanceCommand(
    string Subject,
    string Description,
    Guid RecipientSecurityUserId,
    Guid? SubscriptionId,
    GrievancePriority Priority = GrievancePriority.Medium) : IRequest<Guid>;

public class CreateGrievanceCommandValidator : AbstractValidator<CreateGrievanceCommand>
{
    public CreateGrievanceCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty();
        RuleFor(x => x.RecipientSecurityUserId).NotEmpty();
    }
}

public class CreateGrievanceCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateGrievanceCommand, Guid>
{
    public async Task<Guid> Handle(CreateGrievanceCommand request, CancellationToken ct)
    {
        var actor = await GrievanceAccess.ResolveActorAsync(uow, currentUser, ct);
        var recipient = await GrievanceAccess.ResolveRecipientAsync(uow, request.RecipientSecurityUserId, ct);

        await GrievanceAccess.ValidateCreateScopeAsync(uow, actor, recipient, request.SubscriptionId, ct);

        var companyId = actor.Role switch
        {
            UserRole.SuperAdmin => recipient.CompanyId
                ?? throw new UnauthorizedAccessException("Admin company scope is required for this grievance."),
            _ => actor.CompanyId
                ?? throw new UnauthorizedAccessException("Company scope is required for this grievance.")
        };

        var grievance = new Grievance
        {
            Subject = request.Subject.Trim(),
            Description = request.Description.Trim(),
            CompanyId = companyId,
            SubscriptionId = request.SubscriptionId,
            CreatedBySecurityUserId = actor.SecurityUserId,
            CreatedByUserType = actor.Role,
            RecipientSecurityUserId = recipient.SecurityUser.Id,
            RecipientUserType = recipient.SecurityUser.Role,
            Priority = request.Priority,
            Status = actor.Role == UserRole.SuperAdmin ? GrievanceStatus.Escalated : GrievanceStatus.Open,
            IsActive = true,
            LastMessageAt = DateTime.UtcNow,
            CreatedBy = actor.SecurityUserId.ToString()
        };

        await uow.Grievances.AddAsync(grievance, ct);
        await uow.SaveChangesAsync(ct);
        return grievance.Id;
    }
}
