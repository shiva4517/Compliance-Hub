using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using MediatR;

namespace ComplianceHub.Application.Features.Departments.Commands.UpdateDepartment;

public record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Description
) : IRequest;

public class UpdateDepartmentCommandHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<UpdateDepartmentCommand>
{
    public async Task Handle(UpdateDepartmentCommand request, CancellationToken ct)
    {
        var department = await uow.Departments.GetByIdAsync(request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Department), request.Id);

        var nameExists = await uow.Departments.ExistsAsync(
            d => d.Id != request.Id && d.CompanyId == department.CompanyId && d.Name.ToLower() == request.Name.ToLower(), ct);
        if (nameExists)
            throw new ConflictException($"Department '{request.Name}' already exists for this company.");

        department.Name = request.Name;
        department.Description = request.Description;
        department.UpdatedAt = DateTime.UtcNow;
        department.UpdatedBy = currentUser.Email;

        uow.Departments.Update(department);
        await uow.SaveChangesAsync(ct);
    }
}
