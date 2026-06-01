using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Features.Geo.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/geo")]
public class GeoController : ControllerBase
{
    private readonly IMediator _mediator;

    public GeoController(IMediator mediator) => _mediator = mediator;

    [HttpGet("objects")]
    [ProducesResponseType(typeof(List<MapObjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MapObjectDto>>> GetObjects(
        [FromQuery] string? engineerUserId,
        CancellationToken cancellationToken)
        => await _mediator.Send(new GetMapObjectsQuery(engineerUserId), cancellationToken);

    [HttpGet("objects/emergency")]
    [ProducesResponseType(typeof(List<MapObjectDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<MapObjectDto>>> GetEmergencyObjects(
        [FromQuery] string? engineerUserId,
        CancellationToken cancellationToken)
    {
        var all = await _mediator.Send(new GetMapObjectsQuery(engineerUserId), cancellationToken);
        return all.Where(o => o.HasCriticalTicket).ToList();
    }

    [Authorize(Policy = "CanManageTickets")]
    [HttpGet("nearest-engineers")]
    [ProducesResponseType(typeof(List<NearestEngineerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<NearestEngineerDto>>> GetNearestEngineers(
        [FromQuery] Guid objectId,
        [FromQuery] int top = 5,
        CancellationToken cancellationToken = default)
    {
        return await _mediator.Send(new GetNearestEngineersQuery(objectId, top), cancellationToken);
    }
}
