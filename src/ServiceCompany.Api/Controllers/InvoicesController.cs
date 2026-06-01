using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.Invoices.Commands;
using ServiceCompany.Application.Features.Invoices.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;
    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<InvoiceDto>>> GetAll(
        [FromQuery] GetInvoicesWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDetailDto>> GetById(Guid id, CancellationToken ct)
        => await _mediator.Send(new GetInvoiceByIdQuery(id), ct);

    [Authorize(Policy = "CanManageFinance")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Generate(GenerateInvoiceCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { }, id);
    }

    [Authorize(Policy = "CanManageFinance")]
    [HttpPatch("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, UpdateInvoiceStatusCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageFinance")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult> Update(Guid id, UpdateInvoiceCommand command, CancellationToken ct)
    {
        if (id != command.Id) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageFinance")]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteInvoiceCommand(id), ct);
        return NoContent();
    }
}
