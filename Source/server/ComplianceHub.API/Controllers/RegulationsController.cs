using ComplianceHub.API.Filters;
using ComplianceHub.Application.Common.Interfaces;
using ComplianceHub.Application.Common.Models;
using ComplianceHub.Application.Features.RegulationDetails.Commands.SuggestRegulationDetail;
using ComplianceHub.Application.Features.RegulationDetails.Commands.UpsertAiProviderConnection;
using ComplianceHub.Application.Features.RegulationDetails.Commands.UpsertRegulationDetail;
using ComplianceHub.Application.Features.RegulationDetails.Queries.GetAiProviderConnection;
using ComplianceHub.Application.Features.RegulationDetails.Queries.GetAiProviderModels;
using ComplianceHub.Application.Features.RegulationDetails.Queries.GetRegulationDetail;
using ComplianceHub.Application.Features.Regulations.Commands.CreateGovernmentEntity;
using ComplianceHub.Application.Features.Regulations.Commands.SeedGovernmentEntitiesFromEcfr;
using ComplianceHub.Application.Features.Regulations.Commands.ToggleSyncEnabled;
using ComplianceHub.Application.Features.Regulations.Commands.TriggerManualSync;
using ComplianceHub.Application.Features.Regulations.Commands.UpdateGovernmentEntity;
using ComplianceHub.Application.Features.Regulations.Queries.GetGovernmentEntities;
using ComplianceHub.Application.Features.Regulations.Queries.GetGovernmentEntityById;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationSyncSchedulerHistories;
using ComplianceHub.Application.Features.Regulations.Queries.GetAgencies;
using ComplianceHub.Application.Features.Regulations.Queries.GetCategories;
using ComplianceHub.Application.Features.Regulations.Queries.GetRegulationHierarchy;
using ComplianceHub.Application.Features.Regulations.Queries.GetSectionContent;
using ComplianceHub.Application.Features.Regulations.Queries.GetSections;
using ComplianceHub.Application.Features.Regulations.Queries.GetSimulatedChanges;
using ComplianceHub.Application.Features.Regulations.Commands.ApplySimulatedRegulationChange;
using ComplianceHub.Application.Features.Regulations.Queries.GetSubtypes;
using ComplianceHub.Application.Features.Regulations.Queries.GetTypes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ComplianceHub.API.Services;

namespace ComplianceHub.API.Controllers;

[ApiController]
[Route("api/regulations")]
[Authorize]
public class RegulationsController(IMediator mediator, IRegulationSyncOrchestrator orchestrator, SyncJobStore syncJobStore, IServiceScopeFactory scopeFactory, ILogger<RegulationsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PaginatedList<GovernmentEntityDto>>>> GetAll(
        [FromQuery] string? search, [FromQuery] bool? isSyncEnabled, [FromQuery] bool? isImported,
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        logger.LogInformation(
            "GET /api/regulations bound params: search={Search} isSyncEnabled={IsSyncEnabled} isImported={IsImported} pageNumber={PageNumber} pageSize={PageSize}",
            search, isSyncEnabled, isImported, pageNumber, pageSize);

        var result = await mediator.Send(new GetGovernmentEntitiesQuery(search, isSyncEnabled, isImported, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<GovernmentEntityDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<GovernmentEntityDto>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetGovernmentEntityByIdQuery(id), ct);
        if (result is null) return NotFound(ApiResponse<GovernmentEntityDto>.Fail("Not found."));
        return Ok(ApiResponse<GovernmentEntityDto>.Ok(result));
    }

    [HttpGet("sync-scheduler-history")]
    public async Task<ActionResult<ApiResponse<PaginatedList<RegulationSyncSchedulerHistoryDto>>>> GetSyncSchedulerHistory(
        [FromQuery] string sortBy = "CompletedAt",
        [FromQuery] string sortDir = "desc",
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 0,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetRegulationSyncSchedulerHistoriesQuery(sortBy, sortDir, pageNumber, pageSize), ct);
        return Ok(ApiResponse<PaginatedList<RegulationSyncSchedulerHistoryDto>>.Ok(result));
    }

    [HttpGet("{id:guid}/hierarchy")]
    public async Task<ActionResult<ApiResponse<RegulationHierarchyDto>>> GetHierarchy(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRegulationHierarchyQuery(id), ct);
        if (result is null) return NotFound(ApiResponse<RegulationHierarchyDto>.Fail("Not found."));
        return Ok(ApiResponse<RegulationHierarchyDto>.Ok(result));
    }

    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> Create([FromBody] CreateGovernmentEntityCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, ApiResponse<Guid>.Ok(id, "Government entity created."));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> Update(Guid id, [FromBody] UpdateGovernmentEntityCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest(ApiResponse<object>.Fail("ID mismatch."));
        await mediator.Send(command, ct);
        return Ok(ApiResponse<object>.Ok(null!, "Updated successfully."));
    }

    [HttpPut("{id:guid}/toggle-sync")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<bool>>> ToggleSync(Guid id, CancellationToken ct)
    {
        var enabled = await mediator.Send(new ToggleSyncEnabledCommand(id), ct);
        return Ok(ApiResponse<bool>.Ok(enabled, $"Sync {(enabled ? "enabled" : "disabled")}."));
    }

    [HttpPost("{id:guid}/sync")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<SyncResult>>> TriggerSync(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new TriggerManualSyncCommand(id), ct);
        return Ok(ApiResponse<SyncResult>.Ok(result, "Sync completed."));
    }

    [HttpPost("seed-from-ecfr")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<int>>> SeedFromEcfr(CancellationToken ct)
    {
        var count = await mediator.Send(new SeedGovernmentEntitiesFromEcfrCommand(), ct);
        return Ok(ApiResponse<int>.Ok(count, $"{count} new titles seeded from eCFR."));
    }

    [HttpGet("{regulationId:guid}/detail")]
    public async Task<ActionResult<ApiResponse<RegulationDetailDto>>> GetDetail(Guid regulationId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetRegulationDetailQuery(regulationId), ct);
        if (result is null) return Ok(ApiResponse<RegulationDetailDto?>.Ok(null));
        return Ok(ApiResponse<RegulationDetailDto>.Ok(result));
    }

    [HttpPut("{regulationId:guid}/detail")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<Guid>>> UpsertDetail(Guid regulationId, [FromBody] UpsertRegulationDetailRequest body, CancellationToken ct)
    {
        var id = await mediator.Send(new UpsertRegulationDetailCommand(
            regulationId, body.Description, body.Condition, body.SuggestedTask,
            body.FrequencyTypeId, body.DueDateTypeId), ct);
        return Ok(ApiResponse<Guid>.Ok(id, "Saved successfully."));
    }

    [HttpGet("ai-connection")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<AiProviderConnectionViewDto?>>> GetAiConnection(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAiProviderConnectionQuery(), ct);
        return Ok(ApiResponse<AiProviderConnectionViewDto?>.Ok(result));
    }

    [HttpPut("ai-connection")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<AiProviderConnectionDto>>> UpsertAiConnection(
        [FromBody] UpsertAiProviderConnectionRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new UpsertAiProviderConnectionCommand(
                body.Provider,
                body.ApiKey,
                body.Endpoint,
                body.DeploymentName,
                body.Model,
                body.ApiVersion), ct);

            return Ok(ApiResponse<AiProviderConnectionDto>.Ok(result, "AI provider saved successfully."));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("rate-limiting", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                ApiResponse<AiProviderConnectionDto>.Fail(ex.Message));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ApiResponse<AiProviderConnectionDto>.Fail(ex.Message));
        }
    }

    [HttpPost("ai-models")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AiProviderModelOptionDto>>>> GetAiModels(
        [FromBody] GetAiProviderModelsRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetAiProviderModelsQuery(
                body.Provider,
                body.ApiKey,
                body.Endpoint), ct);

            return Ok(ApiResponse<IReadOnlyList<AiProviderModelOptionDto>>.Ok(result));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("rate-limiting", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                ApiResponse<IReadOnlyList<AiProviderModelOptionDto>>.Fail(ex.Message));
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return BadRequest(ApiResponse<IReadOnlyList<AiProviderModelOptionDto>>.Fail(ex.Message));
        }
    }

    [HttpPost("{regulationId:guid}/detail/suggest")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<SuggestRegulationDetailResponse>>> SuggestDetail(
        Guid regulationId,
        [FromBody] SuggestRegulationDetailRequest body,
        CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new SuggestRegulationDetailCommand(
                regulationId,
                body.Description,
                body.Condition,
                body.SuggestedTask), ct);

            return Ok(ApiResponse<SuggestRegulationDetailResponse>.Ok(result));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("rate-limiting", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(StatusCodes.Status429TooManyRequests,
                ApiResponse<SuggestRegulationDetailResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SuggestRegulationDetailResponse>.Fail(ex.Message));
        }
    }

    // ── Lazy hierarchy endpoints ──────────────────────────────────────────

    [HttpGet("{entityId:guid}/agencies")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgencyHierarchyDto>>>> GetAgencies(Guid entityId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetAgenciesQuery(entityId), ct);
        return Ok(ApiResponse<IReadOnlyList<AgencyHierarchyDto>>.Ok(result));
    }

    [HttpGet("agencies/{agencyId:guid}/categories")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryHierarchyDto>>>> GetCategories(Guid agencyId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCategoriesQuery(agencyId), ct);
        return Ok(ApiResponse<IReadOnlyList<CategoryHierarchyDto>>.Ok(result));
    }

    [HttpGet("categories/{categoryId:guid}/types")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TypeHierarchyDto>>>> GetTypes(Guid categoryId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetTypesQuery(categoryId), ct);
        return Ok(ApiResponse<IReadOnlyList<TypeHierarchyDto>>.Ok(result));
    }

    [HttpGet("types/{typeId:guid}/subtypes")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SubtypeHierarchyDto>>>> GetSubtypes(Guid typeId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSubtypesQuery(typeId), ct);
        return Ok(ApiResponse<IReadOnlyList<SubtypeHierarchyDto>>.Ok(result));
    }

    [HttpGet("subtypes/{subtypeId:guid}/sections")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SectionDto>>>> GetSections(
        Guid subtypeId, [FromQuery] Guid? typeId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSectionsQuery(subtypeId, typeId), ct);
        return Ok(ApiResponse<IReadOnlyList<SectionDto>>.Ok(result));
    }

    [HttpGet("sections/{sectionId:guid}/content")]
    public async Task<ActionResult<ApiResponse<string?>>> GetSectionContent(Guid sectionId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetSectionContentQuery(sectionId), ct);
        return Ok(ApiResponse<string?>.Ok(result));
    }

    [HttpPost("{id:guid}/sync-start")]
    [Authorize(Policy = "Admin")]
    public IActionResult StartSync(Guid id)
    {
        var (jobId, job) = syncJobStore.Create();

        _ = Task.Run(async () =>
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var scopedOrchestrator = scope.ServiceProvider.GetRequiredService<IRegulationSyncOrchestrator>();
            try
            {
                var result = await scopedOrchestrator.SyncGovernmentEntityAsync(
                    id,
                    (msg, level) => { job.AddLog(msg, level.ToString().ToLowerInvariant()); return Task.CompletedTask; },
                    "Manual",
                    CancellationToken.None);

                job.Summary = new SyncResultSnapshot(
                    result.SectionsAdded,
                    result.SectionsUpdated,
                    result.SectionsDeactivated,
                    result.OutboxEventsQueued,
                    result.Duration.ToString(@"hh\:mm\:ss"),
                    result.Error);
                job.Status = SyncJobStatus.Done;
            }
            catch (Exception ex)
            {
                job.Error = ex.Message;
                job.Status = SyncJobStatus.Error;
            }
        });

        return Ok(new { jobId });
    }

    [HttpGet("{id:guid}/sync-status/{jobId:guid}")]
    [Authorize(Policy = "Admin")]
    public IActionResult GetSyncStatus(Guid id, Guid jobId)
    {
        var job = syncJobStore.Get(jobId);
        if (job is null) return NotFound();

        return Ok(new
        {
            status = job.Status.ToString().ToLowerInvariant(),
            logs = job.GetLogs(),
            error = job.Error,
            summary = job.Summary
        });
    }

    // ── Super-Admin: Simulate Regulation Changes ──────────────────────────

    [HttpGet("{governmentEntityId:guid}/simulate/changes")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SimulatedChangesResultDto>>> GetSimulatedChanges(
        Guid governmentEntityId, [FromQuery] DateOnly issueDate, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new GetSimulatedChangesQuery(governmentEntityId, issueDate), ct);
            return Ok(ApiResponse<SimulatedChangesResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(ApiResponse<SimulatedChangesResultDto>.Fail(ex.Message));
        }
    }

    public record ApplySimulatedRegulationChangeRequest(
        Guid RegulationId,
        string HtmlContent,
        DateOnly SimulatedDate);

    [HttpPost("simulate/apply")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<ApplySimulatedRegulationChangeResult>>> ApplySimulatedChange(
        [FromBody] ApplySimulatedRegulationChangeRequest body, CancellationToken ct)
    {
        try
        {
            var result = await mediator.Send(new ApplySimulatedRegulationChangeCommand(
                body.RegulationId, body.HtmlContent, body.SimulatedDate), ct);
            return Ok(ApiResponse<ApplySimulatedRegulationChangeResult>.Ok(result, "Simulated change applied."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ApplySimulatedRegulationChangeResult>.Fail(ex.Message));
        }
    }

    [HttpPost("sync-all")]
    [AllowAnonymous]
    [InternalApiKey]
    public async Task<IActionResult> SyncAll(CancellationToken ct)
    {
        await orchestrator.RunAllEnabledAsync("Manual", ct);
        return Ok();
    }
}

public record UpsertRegulationDetailRequest(
    string? Description,
    string? Condition,
    string? SuggestedTask,
    Guid? FrequencyTypeId,
    Guid? DueDateTypeId);

public record SuggestRegulationDetailRequest(
    string? Description,
    string? Condition,
    string? SuggestedTask);

public record UpsertAiProviderConnectionRequest(
    string Provider,
    string ApiKey,
    string? Endpoint,
    string? DeploymentName,
    string? Model,
    string? ApiVersion);

public record GetAiProviderModelsRequest(
    string Provider,
    string ApiKey,
    string? Endpoint);
