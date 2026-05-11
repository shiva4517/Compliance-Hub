using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Features.Employees.Queries.GetEmployeeAssignedWork;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetAssignedWorkById;

public record GetAssignedWorkByIdQuery(Guid Id) : IRequest<AssignedWorkDto>;

public class GetAssignedWorkByIdQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regulationsUow)
    : IRequestHandler<GetAssignedWorkByIdQuery, AssignedWorkDto>
{
    public async Task<AssignedWorkDto> Handle(GetAssignedWorkByIdQuery request, CancellationToken ct)
    {
        var a = await uow.EmployeeAssignedWorks.Query()
            .Include(x => x.Subscription)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EmployeeAssignedWork), request.Id);

        var sub = a.Subscription!;

        var govEntity = await regulationsUow.GovernmentEntities.Query()
            .Where(e => e.Id == sub.GovernmentEntityId)
            .Select(e => e.TitleName)
            .FirstOrDefaultAsync(ct) ?? "—";

        var agencyName = sub.AgencyId.HasValue
            ? await regulationsUow.Agencies.Query()
                .Where(x => x.Id == sub.AgencyId.Value)
                .Select(x => x.AgencyName)
                .FirstOrDefaultAsync(ct)
            : null;

        var categoryName = sub.RegulationCategoryId.HasValue
            ? await regulationsUow.RegulationCategories.Query()
                .Where(x => x.Id == sub.RegulationCategoryId.Value)
                .Select(x => x.SubchapterName)
                .FirstOrDefaultAsync(ct)
            : null;

        var typeName = sub.RegulationTypeId.HasValue
            ? await regulationsUow.RegulationTypes.Query()
                .Where(x => x.Id == sub.RegulationTypeId.Value)
                .Select(x => x.PartName)
                .FirstOrDefaultAsync(ct)
            : null;

        var subtypeName = sub.RegulationSubtypeId.HasValue
            ? await regulationsUow.RegulationSubtypes.Query()
                .Where(x => x.Id == sub.RegulationSubtypeId.Value)
                .Select(x => x.SubpartName)
                .FirstOrDefaultAsync(ct)
            : null;

        return new AssignedWorkDto(
            a.Id,
            a.EmployeeId,
            a.SubscriptionId,
            a.CustomerId,
            a.Customer!.CustomerName,
            sub.SubscribingLevel,
            sub.SubscribedNodeName,
            sub.GovernmentEntityId,
            govEntity,
            sub.AgencyId,
            agencyName,
            sub.RegulationCategoryId,
            categoryName,
            sub.RegulationTypeId,
            typeName,
            sub.RegulationSubtypeId,
            subtypeName,
            a.IsActive,
            a.AssignedAt,
            a.AssignedBy,
            a.CreatedAt,
            a.CreatedBy);
    }
}
