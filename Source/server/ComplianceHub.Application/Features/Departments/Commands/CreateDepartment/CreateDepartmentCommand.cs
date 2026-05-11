using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using MediatR;

namespace ComplianceHub.Application.Features.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(
    Guid CompanyId,
    string Name,
    string? Description
) : IRequest<Guid>;

public class CreateDepartmentCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<CreateDepartmentCommand, Guid>
{
    public async Task<Guid> Handle(CreateDepartmentCommand request, CancellationToken ct)
    {
        var companyExists = await uow.Companies.ExistsAsync(c => c.Id == request.CompanyId, ct);
        if (!companyExists)
            throw new NotFoundException(nameof(Company), request.CompanyId);

        var nameExists = await uow.Departments.ExistsAsync(
            d => d.CompanyId == request.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"Department '{request.Name}' already exists for this company.");

        var department = new Department
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            Description = request.Description,
            IsActive = true,
            CreatedBy = currentUser.Email
        };

        await uow.Departments.AddAsync(department, ct);
        await uow.SaveChangesAsync(ct);

        return department.Id;
    }
}
