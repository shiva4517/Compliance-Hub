using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;

namespace ComplianceHub.Application.Features.CompanyDivisions.Commands.CreateCompanyDivision;

public record CreateCompanyDivisionCommand(
    Guid CompanyId,
    Guid? DepartmentId,
    string Name,
    string? Description
) : IRequest<Guid>;

public class CreateCompanyDivisionCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateCompanyDivisionCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyDivisionCommand request, CancellationToken ct)
    {
        var companyExists = await uow.Companies.ExistsAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new NotFoundException(nameof(Company), request.CompanyId);

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await uow.Departments.ExistsAsync(
                d => d.Id == request.DepartmentId.Value && d.CompanyId == request.CompanyId, ct);
            if (!deptExists)
                throw new NotFoundException(nameof(Department), request.DepartmentId.Value);
        }

        var nameExists = await uow.CompanyDivisions.ExistsAsync(
            d => d.CompanyId == request.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"Division '{request.Name}' already exists for this company.");

        var division = new CompanyDivision
        {
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.CompanyDivisions.AddAsync(division, ct);
        await uow.SaveChangesAsync(ct);

        return division.Id;
    }
}
