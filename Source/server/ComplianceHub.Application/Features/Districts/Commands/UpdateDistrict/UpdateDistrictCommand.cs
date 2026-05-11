using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Districts.Commands.UpdateDistrict;

public record UpdateDistrictCommand(
    Guid Id,
    string Name,
    string? Description
) : IRequest;

public class UpdateDistrictCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateDistrictCommand>
{
    public async Task Handle(UpdateDistrictCommand request, CancellationToken ct)
    {
        var district = await uow.Districts.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.District), request.Id);

        var nameExists = await uow.Districts.ExistsAsync(
            d => d.Id != request.Id && d.CompanyId == district.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"District '{request.Name}' already exists for this company.");

        district.Name = request.Name;
        district.Description = request.Description;
        district.UpdatedAt = DateTime.UtcNow;
        district.UpdatedBy = currentUser.Email;

        uow.Districts.Update(district);
        await uow.SaveChangesAsync(ct);
    }
}
