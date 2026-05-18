using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.AiPrAutomation.Commands.StartAiPrAutomation;
using ComplianceHub.Application.Features.AiPrAutomation.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/ai-pr-automation")]
[Authorize(Policy = "SuperAdmin")]
public sealed class AiPrAutomationController(IMediator mediator) : ControllerBase
{
    [HttpPost("runs")]
    public async Task<ActionResult<ApiResponse<AiPrAutomationRunResult>>> StartRun(
        [FromBody] StartAiPrAutomationRequest request,
        CancellationToken ct)
    {
        var result = await mediator.Send(new StartAiPrAutomationCommand(request), ct);
        return Ok(ApiResponse<AiPrAutomationRunResult>.Ok(result, "AI PR automation run completed."));
    }
}
