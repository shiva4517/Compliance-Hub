using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.Grievances.Commands.AddGrievanceReply;
using ComplianceHub.Application.Features.Grievances.Commands.CreateGrievance;
using ComplianceHub.Application.Features.Grievances.Queries.GetGrievanceById;
using ComplianceHub.Application.Features.Grievances.Queries.GetGrievanceContacts;
using ComplianceHub.Application.Features.Grievances.Queries.GetGrievances;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/grievances")]
[Authorize]
public class GrievancesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<GrievanceListItemDto>>>> GetAll(CancellationToken ct)
    {
        var result = await mediator.Send(new GetGrievancesQuery(), ct);
        return Ok(ApiResponse<List<GrievanceListItemDto>>.Ok(result));
    }

    [HttpGet("contacts")]
    public async Task<ActionResult<ApiResponse<List<GrievanceContactDto>>>> GetContacts(CancellationToken ct)
    {
        var result = await mediator.Send(new GetGrievanceContactsQuery(), ct);
        return Ok(ApiResponse<List<GrievanceContactDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GrievanceDetailDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGrievanceByIdQuery(id), ct);
        return Ok(ApiResponse<GrievanceDetailDto>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateGrievanceCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return Ok(ApiResponse<Guid>.Ok(id, "Grievance created successfully."));
    }

    [HttpPost("{id:guid}/replies")]
    public async Task<ActionResult<ApiResponse<Guid>>> Reply(Guid id, [FromBody] AddGrievanceReplyRequest body, CancellationToken ct)
    {
        var replyId = await mediator.Send(new AddGrievanceReplyCommand(id, body.Message), ct);
        return Ok(ApiResponse<Guid>.Ok(replyId, "Reply added successfully."));
    }
}

public record AddGrievanceReplyRequest(string Message);
