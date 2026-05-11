using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class DashboardController(IUnitOfWork uow) : ControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<AdminDashboardStats>>> GetStats(
        [FromQuery] Guid companyId, CancellationToken ct)
    {
        var customers = await uow.Customers.Query()
            .Where(c => c.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var employees = await uow.Employees.Query()
            .Where(e => e.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var subscriptions = await uow.Subscriptions.Query()
            .Where(s => s.Customer!.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var divisions = await uow.CompanyDivisions.Query()
            .Where(d => d.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var districts = await uow.Districts.Query()
            .Where(d => d.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var departments = await uow.Departments.Query()
            .Where(d => d.CompanyId == companyId)
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var stats = new AdminDashboardStats(
            customers,
            employees,
            subscriptions,
            divisions,
            districts,
            departments,
            new GrievanceCounts(0, 0, 0));

        return Ok(ApiResponse<AdminDashboardStats>.Ok(stats));
    }

    [HttpGet("super-admin-stats")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminDashboardStats>>> GetSuperAdminStats(CancellationToken ct)
    {
        var companies = await uow.Companies.Query()
            .GroupBy(_ => 1)
            .Select(g => new EntityCounts(
                g.Count(),
                g.Count(x => x.IsActive),
                g.Count(x => !x.IsActive)))
            .FirstOrDefaultAsync(ct) ?? new EntityCounts(0, 0, 0);

        var totalUsers = await uow.SecurityUsers.Query().CountAsync(ct);
        var superAdminCount = await uow.SecurityUsers.Query()
            .CountAsync(u => u.Role == UserRole.SuperAdmin, ct);
        var adminCount = await uow.SecurityUsers.Query()
            .CountAsync(u => u.Role == UserRole.Admin, ct);

        var stats = new SuperAdminDashboardStats(
            companies,
            new UserCounts(totalUsers, superAdminCount, adminCount),
            new GrievanceCounts(0, 0, 0));

        return Ok(ApiResponse<SuperAdminDashboardStats>.Ok(stats));
    }
}

public record EntityCounts(int Total, int Active, int Inactive);
public record GrievanceCounts(int Received, int Replied, int Pending);
public record UserCounts(int Total, int SuperAdminCount, int AdminCount);
public record AdminDashboardStats(
    EntityCounts Customers,
    EntityCounts Employees,
    EntityCounts Subscriptions,
    EntityCounts Divisions,
    EntityCounts Districts,
    EntityCounts Departments,
    GrievanceCounts Grievances);
public record SuperAdminDashboardStats(
    EntityCounts Companies,
    UserCounts Users,
    GrievanceCounts Grievances);
