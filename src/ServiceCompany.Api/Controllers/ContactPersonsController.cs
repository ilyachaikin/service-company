using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Features.ContactPersons.Commands;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class ContactPersonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ContactPersonsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateContactPersonCommand command)
    {
        return await _mediator.Send(command);
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpPut("{id}")]
    public async Task<ActionResult> Update(Guid id, UpdateContactPersonCommand command)
    {
        if (id != command.Id) return BadRequest();
        await _mediator.Send(command);
        return NoContent();
    }

    [Authorize(Policy = "CanManageClients")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        await _mediator.Send(new DeleteContactPersonCommand(id));
        return NoContent();
    }
}
