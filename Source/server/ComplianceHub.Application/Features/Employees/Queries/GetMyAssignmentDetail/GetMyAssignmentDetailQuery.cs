using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Employees.Queries.GetMyAssignmentDetail;

public record GetMyAssignmentDetailQuery(Guid AssignmentId, Guid EmployeeId) : IRequest<MyAssignmentDetailDto>;

public record MyAssignmentDetailDto(
    Guid Id,
    Guid EmployeeId,
    Guid SubscriptionId,
    // Customer info
    Guid CustomerId,
    string CustomerName,
    string CustomerCode,
    string PrimaryContactFirstName,
    string PrimaryContactLastName,
    string? CustomerEmail,
    string? CustomerPhone,
    string? CustomerMobileNumber,
    string? PrimaryAddress,
    string? PrimaryCity,
    string? PrimaryState,
    string? PrimaryPostalCode,
    // Subscription hierarchy
    SubscribingLevel SubscribingLevel,
    string SubscribedNodeName,
    Guid GovernmentEntityId,
    string GovernmentEntityName,
    Guid? AgencyId,
    string? AgencyName,
    Guid? RegulationCategoryId,
    string? RegulationCategoryName,
    Guid? RegulationTypeId,
    string? RegulationTypeName,
    Guid? RegulationSubtypeId,
    string? RegulationSubtypeName,
    Guid? RegulationId,
    // RegulationDetail (optional)
    string? RegulationDescription,
    string? RegulationCondition,
    string? SuggestedTask,
    string? FrequencyTypeName,
    string? DueDateTypeName,
    // Assignment metadata
    bool IsActive,
    DateTime AssignedAt,
    string AssignedBy,
    DateTime CreatedAt,
    string? CreatedBy);

public class GetMyAssignmentDetailQueryHandler(IUnitOfWork uow, IRegulationsUnitOfWork regulationsUow)
    : IRequestHandler<GetMyAssignmentDetailQuery, MyAssignmentDetailDto>
{
    public async Task<MyAssignmentDetailDto> Handle(GetMyAssignmentDetailQuery request, CancellationToken ct)
    {
        var a = await uow.EmployeeAssignedWorks.Query()
            .Include(x => x.Subscription)
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == request.AssignmentId && x.EmployeeId == request.EmployeeId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.EmployeeAssignedWork), request.AssignmentId);

        var sub = a.Subscription!;
        var customer = a.Customer!;

        var govEntityName = await regulationsUow.GovernmentEntities.Query()
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

        string? regulationDescription = null, regulationCondition = null,
                suggestedTask = null, frequencyTypeName = null, dueDateTypeName = null;

        if (sub.RegulationId.HasValue)
        {
            var detail = await uow.RegulationDetails.Query()
                .Include(x => x.FrequencyType)
                .Include(x => x.DueDateType)
                .FirstOrDefaultAsync(x => x.RegulationId == sub.RegulationId.Value, ct);

            if (detail != null)
            {
                regulationDescription = detail.Description;
                regulationCondition = detail.Condition;
                suggestedTask = detail.SuggestedTask;
                frequencyTypeName = detail.FrequencyType?.Name;
                dueDateTypeName = detail.DueDateType?.Name;
            }
        }

        return new MyAssignmentDetailDto(
            a.Id,
            a.EmployeeId,
            a.SubscriptionId,
            customer.Id,
            customer.CustomerName,
            customer.CustomerCode,
            customer.PrimaryContactFirstName,
            customer.PrimaryContactLastName,
            customer.PrimaryEmail,
            customer.PhoneNumber,
            customer.MobileNumber,
            customer.PrimaryAddress,
            customer.PrimaryCity,
            customer.PrimaryState,
            customer.PrimaryPostalCode,
            sub.SubscribingLevel,
            sub.SubscribedNodeName,
            sub.GovernmentEntityId,
            govEntityName,
            sub.AgencyId,
            agencyName,
            sub.RegulationCategoryId,
            categoryName,
            sub.RegulationTypeId,
            typeName,
            sub.RegulationSubtypeId,
            subtypeName,
            sub.RegulationId,
            regulationDescription,
            regulationCondition,
            suggestedTask,
            frequencyTypeName,
            dueDateTypeName,
            a.IsActive,
            a.AssignedAt,
            a.AssignedBy,
            a.CreatedAt,
            a.CreatedBy);
    }
}
