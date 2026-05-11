using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities.Regulations;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Regulations.Commands.CreateGovernmentEntity;

public record CreateGovernmentEntityCommand(
    int TitleNumber,
    string TitleName,
    string? Source,
    bool IsSyncEnabled) : IRequest<Guid>;

public class CreateGovernmentEntityCommandValidator : AbstractValidator<CreateGovernmentEntityCommand>
{
    public CreateGovernmentEntityCommandValidator()
    {
        RuleFor(x => x.TitleNumber).GreaterThan(0);
        RuleFor(x => x.TitleName).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Source).MaximumLength(500).When(x => x.Source is not null);
    }
}

public class CreateGovernmentEntityCommandHandler(IRegulationsUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateGovernmentEntityCommand, Guid>
{
    public async Task<Guid> Handle(CreateGovernmentEntityCommand request, CancellationToken ct)
    {
        var exists = await uow.GovernmentEntities.Query()
            .AnyAsync(e => e.TitleNumber == request.TitleNumber, ct);

        if (exists) throw new InvalidOperationException($"Title {request.TitleNumber} already exists.");

        var entity = new GovernmentEntity
        {
            Identifier = request.TitleNumber.ToString(),
            TitleNumber = request.TitleNumber,
            TitleName = request.TitleName,
            Source = request.Source,
            IsSyncEnabled = request.IsSyncEnabled,
            CreatedBy = currentUser.Email
        };

        await uow.GovernmentEntities.AddAsync(entity, ct);
        await uow.SaveChangesAsync(ct);
        return entity.Id;
    }
}
