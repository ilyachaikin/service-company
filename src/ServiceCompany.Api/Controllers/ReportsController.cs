using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Features.Reports.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize(Policy = "CanViewReports")]
[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ReportsController(IMediator mediator) => _mediator = mediator;

    [HttpGet("sla")]
    public async Task<ActionResult<SlaReportDto>> GetSla(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? clientId,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetSlaReportQuery(from, to, clientId), ct));

    [HttpGet("engineer-workload")]
    public async Task<ActionResult<List<EngineerWorkloadDto>>> GetEngineerWorkload(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetEngineerWorkloadQuery(from, to), ct));

    [HttpGet("problematic-equipment")]
    public async Task<ActionResult<List<ProblematicEquipmentDto>>> GetProblematicEquipment(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] int top = 10,
        CancellationToken ct = default)
        => Ok(await _mediator.Send(new GetProblematicEquipmentQuery(from, to, top), ct));

    [HttpGet("tickets-by-object")]
    public async Task<ActionResult<List<TicketsByObjectDto>>> GetTicketsByObject(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] Guid? clientId,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetTicketsByObjectQuery(from, to, clientId), ct));

    [HttpGet("resolution-by-priority")]
    public async Task<ActionResult<List<AvgResolutionByPriorityDto>>> GetResolutionByPriority(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct)
        => Ok(await _mediator.Send(new GetAvgResolutionByPriorityQuery(from, to), ct));
}
