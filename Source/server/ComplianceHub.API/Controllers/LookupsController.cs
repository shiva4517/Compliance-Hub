using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.DueDateTypes.Queries;
using ComplianceHub.Application.Features.FrequencyTypes.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/lookups")]
[Authorize]
public class LookupsController(IMediator mediator) : ControllerBase
{
    [HttpGet("frequency-types")]
    public async Task<ActionResult<ApiResponse<List<LookupDto>>>> GetFrequencyTypes(CancellationToken ct)
    {
        var result = await mediator.Send(new GetFrequencyTypesQuery(), ct);
        return Ok(ApiResponse<List<LookupDto>>.Ok(result));
    }

    [HttpGet("due-date-types")]
    public async Task<ActionResult<ApiResponse<List<LookupDto>>>> GetDueDateTypes(CancellationToken ct)
    {
        var result = await mediator.Send(new GetDueDateTypesQuery(), ct);
        return Ok(ApiResponse<List<LookupDto>>.Ok(result));
    }
}
