using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Districts.Commands.ToggleDistrictStatus;

public record ToggleDistrictStatusCommand(Guid Id, bool IsActive) : IRequest;

public class ToggleDistrictStatusCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<ToggleDistrictStatusCommand>
{
    public async Task Handle(ToggleDistrictStatusCommand request, CancellationToken ct)
    {
        var district = await uow.Districts.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.District), request.Id);

        district.IsActive = request.IsActive;
        district.UpdatedAt = DateTime.UtcNow;
        district.UpdatedBy = currentUser.Email;

        uow.Districts.Update(district);
        await uow.SaveChangesAsync(ct);
    }
}
