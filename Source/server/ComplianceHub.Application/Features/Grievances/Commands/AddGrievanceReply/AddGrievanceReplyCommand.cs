using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Grievances.Commands.AddGrievanceReply;

public record AddGrievanceReplyCommand(Guid GrievanceId, string Message) : IRequest<Guid>;

public class AddGrievanceReplyCommandValidator : AbstractValidator<AddGrievanceReplyCommand>
{
    public AddGrievanceReplyCommandValidator()
    {
        RuleFor(x => x.GrievanceId).NotEmpty();
        RuleFor(x => x.Message).NotEmpty();
    }
}

public class AddGrievanceReplyCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<AddGrievanceReplyCommand, Guid>
{
    public async Task<Guid> Handle(AddGrievanceReplyCommand request, CancellationToken ct)
    {
        var actor = await GrievanceAccess.ResolveActorAsync(uow, currentUser, ct);

        var grievance = await uow.Grievances.Query()
            .FirstOrDefaultAsync(x => x.Id == request.GrievanceId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Grievance), request.GrievanceId);

        if (!GrievanceAccess.CanReply(actor, grievance))
            throw new UnauthorizedAccessException("You are not allowed to reply to this grievance.");

        var reply = new GrievanceReply
        {
            GrievanceId = grievance.Id,
            SenderSecurityUserId = actor.SecurityUserId,
            SenderUserType = actor.Role,
            Message = request.Message.Trim(),
            CreatedBy = actor.SecurityUserId.ToString()
        };

        grievance.LastMessageAt = DateTime.UtcNow;
        grievance.UpdatedAt = grievance.LastMessageAt;
        grievance.UpdatedBy = actor.SecurityUserId.ToString();
        grievance.Status = actor.Role switch
        {
            UserRole.SuperAdmin => GrievanceStatus.Escalated,
            UserRole.Admin => GrievanceStatus.InProgress,
            _ => grievance.Status == GrievanceStatus.Closed ? GrievanceStatus.Open : grievance.Status
        };

        await uow.GrievanceReplies.AddAsync(reply, ct);
        uow.Grievances.Update(grievance);
        await uow.SaveChangesAsync(ct);
        return reply.Id;
    }
}
