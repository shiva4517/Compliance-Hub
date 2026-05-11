using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;

namespace ComplianceHub.Application.Features.Districts.Commands.CreateDistrict;

public record CreateDistrictCommand(
    Guid CompanyId,
    string Name,
    string? Description
) : IRequest<Guid>;

public class CreateDistrictCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateDistrictCommand, Guid>
{
    public async Task<Guid> Handle(CreateDistrictCommand request, CancellationToken ct)
    {
        var companyExists = await uow.Companies.ExistsAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new NotFoundException(nameof(Company), request.CompanyId);

        var nameExists = await uow.Districts.ExistsAsync(
            d => d.CompanyId == request.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"District '{request.Name}' already exists for this company.");

        var district = new District
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.Districts.AddAsync(district, ct);
        await uow.SaveChangesAsync(ct);

        return district.Id;
    }
}
