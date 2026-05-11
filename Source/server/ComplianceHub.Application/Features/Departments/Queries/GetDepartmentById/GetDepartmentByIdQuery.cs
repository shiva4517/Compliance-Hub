using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Departments.Queries.GetDepartments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Departments.Queries.GetDepartmentById;

public record GetDepartmentByIdQuery(Guid Id) : IRequest<DepartmentDto>;

public class GetDepartmentByIdQueryHandler(IUnitOfWork uow)
    : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken ct)
    {
        var d = await uow.Departments.Query()
            .Include(x => x.Divisions)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.Department), request.Id);

        return new DepartmentDto(d.Id, d.CompanyId, d.Name, d.Description, d.IsActive,
            d.Divisions.Count(), d.CreatedAt);
    }
}
