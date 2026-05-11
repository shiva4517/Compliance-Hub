using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Grievances.Queries.GetGrievanceContacts;

public record GrievanceRecipientSubscriptionDto(Guid Id, string Name);

public record GrievanceContactDto(
    Guid SecurityUserId,
    Guid? UserId,
    string FullName,
    string Role,
    Guid? CompanyId,
    string? CompanyName,
    List<GrievanceRecipientSubscriptionDto> AvailableSubscriptions);

public record GetGrievanceContactsQuery : IRequest<List<GrievanceContactDto>>;

public class GetGrievanceContactsQueryHandler(IUnitOfWork uow, ICurrentUserService currentUser)
    : IRequestHandler<GetGrievanceContactsQuery, List<GrievanceContactDto>>
{
    public async Task<List<GrievanceContactDto>> Handle(GetGrievanceContactsQuery request, CancellationToken ct)
    {
        var actor = await GrievanceAccess.ResolveActorAsync(uow, currentUser, ct);

        return actor.Role switch
        {
            UserRole.SuperAdmin => await GetSuperAdminContacts(ct),
            UserRole.Admin => await GetAdminContacts(actor, ct),
            UserRole.Customer => await GetCustomerContacts(actor, ct),
            UserRole.Employee => await GetEmployeeContacts(actor, ct),
            _ => []
        };
    }

    private async Task<List<GrievanceContactDto>> GetSuperAdminContacts(CancellationToken ct)
    {
        var adminUsers = await uow.SecurityUsers.Query()
    .Where(u => u.Role == UserRole.Admin && u.IsActive && u.UserId != null)
    .Join(
        uow.Companies.Query().Where(c => c.IsActive),
        user => user.UserId!.Value,
        company => company.Id,
        (user, company) => new
        {
            user.Id,
            user.UserId,
            FullName = user.FirstName + " " + user.LastName,
            Role = user.Role.ToString(),
            CompanyId = company.Id,
            company.CompanyName
        })
    .OrderBy(x => x.CompanyName)
    .ThenBy(x => x.FullName)
    .ToListAsync(ct);

        var result = adminUsers
            .Select(x => new GrievanceContactDto(
                x.Id,
                x.UserId,
                x.FullName,
                x.Role,
                x.CompanyId,
                x.CompanyName,
                new List<GrievanceRecipientSubscriptionDto>()))
            .ToList();

        return result;

        //return adminUsers;
    }

    private async Task<List<GrievanceContactDto>> GetAdminContacts(GrievanceActorContext actor, CancellationToken ct)
    {
        var contacts = new List<GrievanceContactDto>();

        var superAdmins = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.SuperAdmin && u.IsActive)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new GrievanceContactDto(
                u.Id,
                u.Id,
                u.FullName,
                u.Role.ToString(),
                null,
                null,
                new List<GrievanceRecipientSubscriptionDto>()))
            .ToListAsync(ct);

        contacts.AddRange(superAdmins);

        var customers = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Customer && u.IsActive && u.UserId != null)
            .Join(
                uow.Customers.Query().Where(c => actor.CompanyId.HasValue && c.CompanyId == actor.CompanyId.Value && c.IsActive),
                user => user.UserId!.Value,
                customer => customer.Id,
                (user, customer) => new GrievanceContactDto(
                    user.Id,
                    customer.Id,
                    user.FullName,
                    user.Role.ToString(),
                    customer.CompanyId,
                    customer.Company.CompanyName,
                    new List<GrievanceRecipientSubscriptionDto>()))
            .ToListAsync(ct);

        contacts.AddRange(customers);

        var employees = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Employee && u.IsActive && u.UserId != null)
            .Join(
                uow.Employees.Query().Where(e => actor.CompanyId.HasValue && e.CompanyId == actor.CompanyId.Value && e.IsActive),
                user => user.UserId!.Value,
                employee => employee.Id,
                (user, employee) => new GrievanceContactDto(
                    user.Id,
                    employee.Id,
                    user.FullName,
                    user.Role.ToString(),
                    employee.CompanyId,
                    employee.Company.CompanyName,
                    new List<GrievanceRecipientSubscriptionDto>()))
            .ToListAsync(ct);

        contacts.AddRange(employees);
        return contacts.OrderBy(x => x.Role).ThenBy(x => x.FullName).ToList();
    }

    private async Task<List<GrievanceContactDto>> GetCustomerContacts(GrievanceActorContext actor, CancellationToken ct)
    {
        if (!actor.HasScopedAssignments || actor.CompanyId is not Guid companyId)
            return [];

        var contacts = new List<GrievanceContactDto>();

        var adminContacts = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Admin && u.IsActive && u.UserId == companyId)
            .Select(u => new GrievanceContactDto(
                u.Id,
                u.UserId,
                u.FullName,
                u.Role.ToString(),
                companyId,
                null,
                new List<GrievanceRecipientSubscriptionDto>()))
            .ToListAsync(ct);

        contacts.AddRange(adminContacts);

        var assignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Employee)
            .Include(a => a.Subscription)
            .Where(a =>
                a.CustomerId == actor.EffectiveUserId &&
                a.IsActive &&
                a.Subscription != null &&
                a.Subscription.IsActive &&
                a.Employee != null &&
                a.Employee.IsActive)
            .ToListAsync(ct);

        var employeeIds = assignments.Select(a => a.EmployeeId).Distinct().ToList();

        var employeeUsers = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Employee && u.IsActive && u.UserId != null && employeeIds.Contains(u.UserId.Value))
            .ToListAsync(ct);

        var employeeContacts = employeeUsers.Select(user =>
        {
            var availableSubscriptions = assignments
                .Where(a => a.EmployeeId == user.UserId)
                .Select(a => new GrievanceRecipientSubscriptionDto(a.SubscriptionId, a.Subscription!.SubscribedNodeName))
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return new GrievanceContactDto(
                user.Id,
                user.UserId,
                user.FullName,
                user.Role.ToString(),
                companyId,
                null,
                availableSubscriptions);
        }).OrderBy(x => x.FullName).ToList();

        contacts.AddRange(employeeContacts);
        return contacts;
    }

    private async Task<List<GrievanceContactDto>> GetEmployeeContacts(GrievanceActorContext actor, CancellationToken ct)
    {
        if (!actor.HasScopedAssignments || actor.CompanyId is not Guid companyId)
            return [];

        var contacts = new List<GrievanceContactDto>();

        var adminContacts = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Admin && u.IsActive && u.UserId == companyId)
            .Select(u => new GrievanceContactDto(
                u.Id,
                u.UserId,
                u.FullName,
                u.Role.ToString(),
                companyId,
                null,
                new List<GrievanceRecipientSubscriptionDto>()))
            .ToListAsync(ct);

        contacts.AddRange(adminContacts);

        var assignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Customer)
            .Include(a => a.Subscription)
            .Where(a =>
                a.EmployeeId == actor.EffectiveUserId &&
                a.IsActive &&
                a.Subscription != null &&
                a.Subscription.IsActive &&
                a.Customer != null &&
                a.Customer.IsActive)
            .ToListAsync(ct);

        var customerIds = assignments.Select(a => a.CustomerId).Distinct().ToList();

        var customerUsers = await uow.SecurityUsers.Query()
            .Where(u => u.Role == UserRole.Customer && u.IsActive && u.UserId != null && customerIds.Contains(u.UserId.Value))
            .ToListAsync(ct);

        var customerContacts = customerUsers.Select(user =>
        {
            var availableSubscriptions = assignments
                .Where(a => a.CustomerId == user.UserId)
                .Select(a => new GrievanceRecipientSubscriptionDto(a.SubscriptionId, a.Subscription!.SubscribedNodeName))
                .DistinctBy(x => x.Id)
                .OrderBy(x => x.Name)
                .ToList();

            return new GrievanceContactDto(
                user.Id,
                user.UserId,
                user.FullName,
                user.Role.ToString(),
                companyId,
                null,
                availableSubscriptions);
        }).OrderBy(x => x.FullName).ToList();

        contacts.AddRange(customerContacts);
        return contacts;
    }
}
