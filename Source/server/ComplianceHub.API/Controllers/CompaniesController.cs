using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Companies.Commands.CreateCompany;
using ComplianceHub.Application.Features.Companies.Commands.DeleteCompany;
using ComplianceHub.Application.Features.Companies.Commands.RestoreCompany;
using ComplianceHub.Application.Features.Companies.Commands.UpdateCompany;
using ComplianceHub.Application.Features.Companies.Queries.GetCompanies;
using ComplianceHub.Application.Features.Companies.Queries.GetCompanyById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CompaniesController(IMediator mediator, IUnitOfWork uow) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<CompanyDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCompaniesQuery(search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<CompanyDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyByIdQuery(id), ct);
        return Ok(ApiResponse<CompanyDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<CreateCompanyResponse>>> Create(
        [FromBody] CreateCompanyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.CompanyId },
            ApiResponse<CreateCompanyResponse>.Ok(result, "Company created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateCompanyCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Company updated successfully."));
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid id, [FromBody] RestoreCompanyCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        var code = await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(new { companyCode = code }, "Company restored successfully."));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteCompanyCommand(id), ct);
        return Ok(ApiResponse<object>.Ok(new { }, "Company deleted successfully."));
    }

    [HttpGet("{id:guid}/summary")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<CompanySummary>>> GetSummary(Guid id, CancellationToken ct)
    {
        var company = await mediator.Send(new GetCompanyByIdQuery(id), ct);

        var employeeCount = await uow.Employees.Query()
            .CountAsync(e => e.CompanyId == id && !e.IsDeleted, ct);

        var customerCount = await uow.Customers.Query()
            .CountAsync(c => c.CompanyId == id && !c.IsDeleted, ct);

        return Ok(ApiResponse<CompanySummary>.Ok(
            new CompanySummary(company, employeeCount, customerCount)));
    }
}

public record CompanySummary(CompanyDto Company, int EmployeeCount, int CustomerCount);
