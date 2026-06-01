using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.Clients.Commands;
using ServiceCompany.Application.Features.Clients.Queries;
using ServiceCompany.Application.Features.ContactPersons.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IMediator _mediator;
    public ClientsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<ClientDto>>> GetAll(
        [FromQuery] GetClientsWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken ct)
        => await _mediator.Send(new GetClientByIdQuery(id), ct);

    [HttpGet("{clientId:guid}/contacts")]
    public async Task<ActionResult<List<ContactPersonDto>>> GetContacts(Guid clientId, CancellationToken ct)
        => await _mediator.Send(new GetContactPersonsByClientQuery(clientId), ct);

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateClientCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateClientCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteClientCommand(id), ct);
        return NoContent();
    }
}
