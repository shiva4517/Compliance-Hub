using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.UpdateCompanyDivision;

public record UpdateCompanyDivisionCommand(
    Guid Id,
    Guid? DepartmentId,
    string Name,
    string? Description
) : IRequest;

public class UpdateCompanyDivisionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateCompanyDivisionCommand>
{
    public async Task Handle(UpdateCompanyDivisionCommand request, CancellationToken ct)
    {
        var division = await uow.CompanyDivisions.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.CompanyDivision), request.Id);

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await uow.Departments.ExistsAsync(
                d => d.Id == request.DepartmentId.Value && d.CompanyId == division.CompanyId, ct);
            if (!deptExists)
                throw new NotFoundException(nameof(Domain.Entities.Department), request.DepartmentId.Value);
        }

        var nameExists = await uow.CompanyDivisions.ExistsAsync(
            d => d.Id != request.Id && d.CompanyId == division.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"Division '{request.Name}' already exists for this company.");

        division.DepartmentId = request.DepartmentId;
        division.Name = request.Name;
        division.Description = request.Description;
        division.UpdatedAt = DateTime.UtcNow;
        division.UpdatedBy = currentUser.Email;

        uow.CompanyDivisions.Update(division);
        await uow.SaveChangesAsync(ct);
    }
}
