using ComplianceHub.Application.Common.Exceptions;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Domain.Entities;
using ComplianceHub.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.Application.Features.Grievances;

internal sealed record GrievanceActorContext(
    Guid SecurityUserId,
    Guid EffectiveUserId,
    UserRole Role,
    Guid? CompanyId,
    HashSet<Guid> AllowedCounterpartyUserIds,
    HashSet<Guid> AllowedSubscriptionIds)
{
    public bool HasScopedAssignments => AllowedSubscriptionIds.Count > 0;
}

internal sealed record GrievanceRecipientContext(
    SecurityUser SecurityUser,
    Guid? CompanyId,
    Guid? EffectiveUserId);

internal static class GrievanceAccess
{
    public static async Task<GrievanceActorContext> ResolveActorAsync(
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        CancellationToken ct)
    {
        var currentUserId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("User not authenticated.");

        if (!Enum.TryParse<UserRole>(currentUser.Role, true, out var role))
            throw new UnauthorizedAccessException("Invalid user role.");

        var securityUser = await uow.SecurityUsers.Query()
            .FirstOrDefaultAsync(u =>
                u.IsActive &&
                u.Role == role &&
                (u.Id == currentUserId || u.UserId == currentUserId), ct)
            ?? throw new UnauthorizedAccessException("Active user account not found.");

        if (role == UserRole.SuperAdmin)
        {
            return new GrievanceActorContext(
                securityUser.Id,
                securityUser.Id,
                role,
                null,
                [],
                []);
        }

        if (securityUser.UserId is not Guid effectiveUserId)
            throw new UnauthorizedAccessException("User scope mapping is invalid.");

        if (role == UserRole.Admin)
        {
            return new GrievanceActorContext(
                securityUser.Id,
                effectiveUserId,
                role,
                effectiveUserId,
                [],
                []);
        }

        if (role == UserRole.Customer)
        {
            var customer = await uow.Customers.Query()
                .FirstOrDefaultAsync(c => c.Id == effectiveUserId && c.IsActive, ct)
                ?? throw new UnauthorizedAccessException("Customer profile is inactive.");

            var assignments = await uow.EmployeeAssignedWorks.Query()
                .Include(a => a.Subscription)
                .Include(a => a.Employee)
                .Where(a =>
                    a.CustomerId == customer.Id &&
                    a.IsActive &&
                    a.Subscription != null &&
                    a.Subscription.IsActive &&
                    a.Employee != null &&
                    a.Employee.IsActive)
                .ToListAsync(ct);

            return new GrievanceActorContext(
                securityUser.Id,
                customer.Id,
                role,
                customer.CompanyId,
                assignments.Select(a => a.EmployeeId).ToHashSet(),
                assignments.Select(a => a.SubscriptionId).ToHashSet());
        }

        var employee = await uow.Employees.Query()
            .FirstOrDefaultAsync(e => e.Id == effectiveUserId && e.IsActive, ct)
            ?? throw new UnauthorizedAccessException("Employee profile is inactive.");

        var employeeAssignments = await uow.EmployeeAssignedWorks.Query()
            .Include(a => a.Subscription)
            .Include(a => a.Customer)
            .Where(a =>
                a.EmployeeId == employee.Id &&
                a.IsActive &&
                a.Subscription != null &&
                a.Subscription.IsActive &&
                a.Customer != null &&
                a.Customer.IsActive)
            .ToListAsync(ct);

        return new GrievanceActorContext(
            securityUser.Id,
            employee.Id,
            role,
            employee.CompanyId,
            employeeAssignments.Select(a => a.CustomerId).ToHashSet(),
            employeeAssignments.Select(a => a.SubscriptionId).ToHashSet());
    }

    public static async Task<GrievanceRecipientContext> ResolveRecipientAsync(
        IUnitOfWork uow,
        Guid recipientSecurityUserId,
        CancellationToken ct)
    {
        var securityUser = await uow.SecurityUsers.Query()
            .FirstOrDefaultAsync(u => u.Id == recipientSecurityUserId && u.IsActive, ct)
            ?? throw new NotFoundException(nameof(SecurityUser), recipientSecurityUserId);

        if (securityUser.Role == UserRole.SuperAdmin)
        {
            return new GrievanceRecipientContext(securityUser, null, securityUser.Id);
        }

        if (securityUser.UserId is not Guid effectiveUserId)
            throw new UnauthorizedAccessException("Recipient scope mapping is invalid.");

        if (securityUser.Role == UserRole.Admin)
        {
            return new GrievanceRecipientContext(securityUser, effectiveUserId, effectiveUserId);
        }

        Guid? companyId = securityUser.Role switch
        {
            UserRole.Customer => await uow.Customers.Query()
                .Where(c => c.Id == effectiveUserId && c.IsActive)
                .Select(c => (Guid?)c.CompanyId)
                .FirstOrDefaultAsync(ct),
            UserRole.Employee => await uow.Employees.Query()
                .Where(e => e.Id == effectiveUserId && e.IsActive)
                .Select(e => (Guid?)e.CompanyId)
                .FirstOrDefaultAsync(ct),
            _ => null
        };

        return new GrievanceRecipientContext(securityUser, companyId, effectiveUserId);
    }

    public static async Task ValidateCreateScopeAsync(
        IUnitOfWork uow,
        GrievanceActorContext actor,
        GrievanceRecipientContext recipient,
        Guid? subscriptionId,
        CancellationToken ct)
    {
        switch (actor.Role)
        {
            case UserRole.SuperAdmin:
                if (recipient.SecurityUser.Role != UserRole.Admin)
                    throw new UnauthorizedAccessException("Super Admin can communicate only with Admin users.");
                return;

            case UserRole.Admin:
                if (actor.CompanyId is not Guid adminCompanyId)
                    throw new UnauthorizedAccessException("Admin company scope is invalid.");

                if (recipient.SecurityUser.Role == UserRole.SuperAdmin)
                    return;

                if (recipient.SecurityUser.Role is not (UserRole.Customer or UserRole.Employee))
                    throw new UnauthorizedAccessException("Admin can communicate only with Super Admin, Customers, and Employees.");

                if (recipient.CompanyId != adminCompanyId)
                    throw new UnauthorizedAccessException("Admin can communicate only within the same company.");
                return;

            case UserRole.Customer:
                if (!actor.HasScopedAssignments)
                    throw new UnauthorizedAccessException("Customer grievances require at least one active employee assignment.");

                if (recipient.CompanyId != actor.CompanyId)
                    throw new UnauthorizedAccessException("Customer can communicate only within the same company.");

                if (recipient.SecurityUser.Role == UserRole.Admin)
                    return;

                if (recipient.SecurityUser.Role != UserRole.Employee || recipient.EffectiveUserId is not Guid employeeId || !actor.AllowedCounterpartyUserIds.Contains(employeeId))
                    throw new UnauthorizedAccessException("Customer can communicate only with assigned employees.");

                if (subscriptionId is not Guid customerSubscriptionId)
                    throw new UnauthorizedAccessException("A subscription is required when contacting an assigned employee.");

                var customerAssignmentExists = await uow.EmployeeAssignedWorks.Query()
                    .Include(a => a.Subscription)
                    .AnyAsync(a =>
                        a.CustomerId == actor.EffectiveUserId &&
                        a.EmployeeId == employeeId &&
                        a.SubscriptionId == customerSubscriptionId &&
                        a.IsActive &&
                        a.Subscription != null &&
                        a.Subscription.IsActive, ct);

                if (!customerAssignmentExists)
                    throw new UnauthorizedAccessException("The selected subscription is not assigned to the target employee.");
                return;

            case UserRole.Employee:
                if (!actor.HasScopedAssignments)
                    throw new UnauthorizedAccessException("Employee grievances require at least one active customer assignment.");

                if (recipient.CompanyId != actor.CompanyId)
                    throw new UnauthorizedAccessException("Employee can communicate only within the same company.");

                if (recipient.SecurityUser.Role == UserRole.Admin)
                    return;

                if (recipient.SecurityUser.Role != UserRole.Customer || recipient.EffectiveUserId is not Guid customerId || !actor.AllowedCounterpartyUserIds.Contains(customerId))
                    throw new UnauthorizedAccessException("Employee can communicate only with assigned customers.");

                if (subscriptionId is not Guid employeeSubscriptionId)
                    throw new UnauthorizedAccessException("A subscription is required when contacting an assigned customer.");

                var employeeAssignmentExists = await uow.EmployeeAssignedWorks.Query()
                    .Include(a => a.Subscription)
                    .AnyAsync(a =>
                        a.EmployeeId == actor.EffectiveUserId &&
                        a.CustomerId == customerId &&
                        a.SubscriptionId == employeeSubscriptionId &&
                        a.IsActive &&
                        a.Subscription != null &&
                        a.Subscription.IsActive, ct);

                if (!employeeAssignmentExists)
                    throw new UnauthorizedAccessException("The selected subscription is not assigned to the target customer.");
                return;

            default:
                throw new UnauthorizedAccessException("Unsupported grievance scope.");
        }
    }

    public static bool CanView(GrievanceActorContext actor, Grievance grievance)
    {
        return actor.Role switch
        {
            UserRole.SuperAdmin =>
                grievance.CreatedByUserType is UserRole.SuperAdmin or UserRole.Admin &&
                grievance.RecipientUserType is UserRole.SuperAdmin or UserRole.Admin,

            UserRole.Admin =>
                actor.CompanyId.HasValue &&
                grievance.CompanyId == actor.CompanyId.Value,

            UserRole.Customer =>
                actor.CompanyId.HasValue &&
                grievance.CompanyId == actor.CompanyId.Value &&
                actor.HasScopedAssignments &&
                (grievance.CreatedBySecurityUserId == actor.SecurityUserId || grievance.RecipientSecurityUserId == actor.SecurityUserId) &&
                (grievance.SubscriptionId == null || actor.AllowedSubscriptionIds.Contains(grievance.SubscriptionId.Value)),

            UserRole.Employee =>
                actor.CompanyId.HasValue &&
                grievance.CompanyId == actor.CompanyId.Value &&
                actor.HasScopedAssignments &&
                (grievance.CreatedBySecurityUserId == actor.SecurityUserId || grievance.RecipientSecurityUserId == actor.SecurityUserId) &&
                (grievance.SubscriptionId == null || actor.AllowedSubscriptionIds.Contains(grievance.SubscriptionId.Value)),

            _ => false
        };
    }

    public static bool CanReply(GrievanceActorContext actor, Grievance grievance)
    {
        if (!CanView(actor, grievance))
            return false;

        return actor.Role switch
        {
            UserRole.SuperAdmin => true,
            UserRole.Admin => true,
            UserRole.Customer => grievance.CreatedBySecurityUserId == actor.SecurityUserId || grievance.RecipientSecurityUserId == actor.SecurityUserId,
            UserRole.Employee => grievance.CreatedBySecurityUserId == actor.SecurityUserId || grievance.RecipientSecurityUserId == actor.SecurityUserId,
            _ => false
        };
    }
}
