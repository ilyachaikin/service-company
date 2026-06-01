using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.MaintenancePlans.Commands;
using ServiceCompany.Application.Features.MaintenancePlans.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/maintenance-plans")]
public class MaintenancePlansController : ControllerBase
{
    private readonly IMediator _mediator;
    public MaintenancePlansController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<MaintenancePlanDto>>> GetAll(
        [FromQuery] GetMaintenancePlansQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [Authorize(Policy = "CanManageTickets")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateMaintenancePlanCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageTickets")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateMaintenancePlanCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageTickets")]
    [HttpPatch("{id:guid}/toggle")]
    public async Task<ActionResult<bool>> Toggle(Guid id, CancellationToken ct)
    {
        var isActive = await _mediator.Send(new ToggleMaintenancePlanCommand(id), ct);
        return Ok(new { IsActive = isActive });
    }

    [Authorize(Policy = "CanManageTickets")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteMaintenancePlanCommand(id), ct);
        return NoContent();
    }
}
