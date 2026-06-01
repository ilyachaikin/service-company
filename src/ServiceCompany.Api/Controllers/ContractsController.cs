using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.Contracts.Commands;
using ServiceCompany.Application.Features.Contracts.Queries;
using ServiceCompany.Application.Features.SlaPolicies.Commands;
using ServiceCompany.Application.Features.SlaPolicies.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ContractsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ContractDto>>> GetAll(
        [FromQuery] GetContractsWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateContractCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateContractCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteContractCommand(id), ct);
        return NoContent();
    }
}

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class SlaPoliciesController : ControllerBase
{
    private readonly IMediator _mediator;
    public SlaPoliciesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<List<SlaPolicyDto>>> GetAll(CancellationToken ct)
        => await _mediator.Send(new GetSlaPoliciesQuery(), ct);

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateSlaPolicyCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateSlaPolicyCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteSlaPolicyCommand(id), ct);
        return NoContent();
    }
}
