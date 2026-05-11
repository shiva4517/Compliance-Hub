using ComplianceHub.API.Models;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Customers.Commands.CreateCustomer;
using ComplianceHub.Application.Features.Customers.Commands.DeleteCustomer;
using ComplianceHub.Application.Features.Customers.Commands.RestoreCustomer;
using ComplianceHub.Application.Features.Customers.Commands.ToggleCustomerStatus;
using ComplianceHub.Application.Features.Customers.Commands.UpdateCustomer;
using ComplianceHub.Application.Features.Customers.Queries.GetCurrentCustomerProfile;
using ComplianceHub.Application.Features.Customers.Queries.GetCustomerById;
using ComplianceHub.Application.Features.Customers.Queries.GetCustomerSubscriptions;
using ComplianceHub.Application.Features.Customers.Queries.GetCustomers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomersController(IMediator mediator, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = "Customer")]
    public async Task<ActionResult<ApiResponse<CurrentCustomerProfileDto>>> GetCurrent(CancellationToken ct)
    {
        if (currentUser.UserId is not Guid securityUserId)
            return Unauthorized(ApiResponse<CurrentCustomerProfileDto>.Fail("Unauthorized access."));

        var result = await mediator.Send(new GetCurrentCustomerProfileQuery(securityUserId), ct);
        return Ok(ApiResponse<CurrentCustomerProfileDto>.Ok(result));
    }

    [HttpGet]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<PaginatedList<CustomerDto>>>> GetAll(
        [FromQuery] Guid companyId,
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetCustomersQuery(companyId, search, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<CustomerDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<CustomerDto>>> GetById(
        Guid id, [FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerByIdQuery(id, companyId), ct);
        return Ok(ApiResponse<CustomerDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create(
        [FromBody] CreateCustomerCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.Ok(id, "Customer created successfully."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(
        Guid id, [FromBody] UpdateCustomerCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Customer updated successfully."));
    }

    [HttpPost("{id:guid}/restore")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Restore(
        Guid id, [FromBody] RestoreCustomerCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Customer restored successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleStatus(
        Guid id, [FromBody] ToggleStatusRequest body, [FromQuery] Guid companyId, CancellationToken ct)
    {
        await mediator.Send(new ToggleCustomerStatusCommand(id, companyId, body.IsActive), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Status updated."));
    }

    [HttpGet("{id:guid}/subscriptions")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<List<CustomerSubscriptionDto>>>> GetSubscriptions(
        Guid id, [FromQuery] Guid companyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCustomerSubscriptionsQuery(id, companyId), ct);
        return Ok(ApiResponse<List<CustomerSubscriptionDto>>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(
        Guid id, [FromQuery] Guid companyId, CancellationToken ct)
    {
        await mediator.Send(new DeleteCustomerCommand(id, companyId), ct);
        return Ok(ApiResponse<object>.Ok(null!, "Customer deleted successfully."));
    }
}
