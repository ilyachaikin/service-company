using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.ServiceObjects.Commands;
using ServiceCompany.Application.Features.ServiceObjects.Queries;
using ServiceCompany.Application.Features.Equipments.Commands;
using ServiceCompany.Application.Features.Equipments.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ServiceObjectsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ServiceObjectsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ServiceObjectDto>>> GetAll(
        [FromQuery] GetServiceObjectsWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateServiceObjectCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateServiceObjectCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteServiceObjectCommand(id), ct);
        return NoContent();
    }
}

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class EquipmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    public EquipmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<EquipmentDto>>> GetAll(
        [FromQuery] GetEquipmentsWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateEquipmentCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateEquipmentCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteEquipmentCommand(id), ct);
        return NoContent();
    }
}
