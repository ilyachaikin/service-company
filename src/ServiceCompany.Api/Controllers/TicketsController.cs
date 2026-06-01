using ClosedXML.Excel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Application.Features.Tickets.Commands;
using ServiceCompany.Application.Features.Tickets.Queries;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class TicketsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IIdentityService _identityService;

    public TicketsController(IMediator mediator, IIdentityService identityService)
    {
        _mediator = mediator;
        _identityService = identityService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResult<TicketDto>>> GetAll(
        [FromQuery] GetTicketsWithPaginationQuery query, CancellationToken ct)
        => await _mediator.Send(query, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketDetailsDto>> GetById(Guid id, CancellationToken ct)
        => await _mediator.Send(new GetTicketByIdQuery(id), ct);

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateTicketCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult> UpdateStatus(Guid id, UpdateTicketStatusCommand command, CancellationToken ct)
    {
        if (id != command.TicketId) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [Authorize(Policy = "CanManageTickets")]
    [HttpPut("{id:guid}/assign")]
    public async Task<ActionResult> Assign(Guid id, AssignTicketCommand command, CancellationToken ct)
    {
        if (id != command.TicketId) return BadRequest("ID в маршруте и теле запроса не совпадают");
        await _mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<Guid>> AddComment(Guid id, AddTicketCommentCommand command, CancellationToken ct)
    {
        if (id != command.TicketId) return BadRequest("ID в маршруте и теле запроса не совпадают");
        return await _mediator.Send(command, ct);
    }

    [HttpGet("engineers")]
    public async Task<ActionResult<List<UserDto>>> GetEngineers()
    {
        var engineers = await _identityService.GetUsersByRoleAsync("Engineer");
        var managers  = await _identityService.GetUsersByRoleAsync("Manager");
        var admins    = await _identityService.GetUsersByRoleAsync("Admin");

        return engineers.Concat(managers).Concat(admins)
            .DistinctBy(u => u.Id)
            .OrderBy(u => u.FullName)
            .ToList();
    }

    [HttpGet("export/xlsx")]
    public async Task<IActionResult> ExportXlsx([FromQuery] GetTicketsWithPaginationQuery query, CancellationToken ct)
    {

        query.Page = 1;
        query.PageSize = 10000;
        var result = await _mediator.Send(query, ct);

        var statusLabels = new Dictionary<int, string>
        {
            [0] = "Новая", [1] = "Назначена", [2] = "В работе",
            [3] = "Ожидание запчастей", [4] = "Выполнена", [5] = "Закрыта"
        };
        var priorityLabels = new Dictionary<int, string>
        {
            [0] = "Критический", [1] = "Высокий", [2] = "Обычный", [3] = "Низкий"
        };

        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Заявки");

        var headers = new[] { "№", "Тема", "Клиент", "Объект", "Статус", "Приоритет", "Исполнитель", "Дата создания", "Срок исполнения", "Закрыта" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1890ff");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 2;
        foreach (var ticket in result.Items)
        {
            ws.Cell(row, 1).Value = row - 1;
            ws.Cell(row, 2).Value = ticket.Title;
            ws.Cell(row, 3).Value = ticket.ClientName;
            ws.Cell(row, 4).Value = ticket.ServiceObjectName ?? "—";
            ws.Cell(row, 5).Value = statusLabels.TryGetValue((int)ticket.Status, out var sl) ? sl : ticket.Status.ToString();
            ws.Cell(row, 6).Value = priorityLabels.TryGetValue((int)ticket.Priority, out var pl) ? pl : ticket.Priority.ToString();
            ws.Cell(row, 7).Value = ticket.AssignedUserName ?? "Не назначен";
            ws.Cell(row, 8).Value = ticket.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            ws.Cell(row, 9).Value = ticket.DueDate.HasValue ? ticket.DueDate.Value.ToString("dd.MM.yyyy") : "—";
            ws.Cell(row, 10).Value = ticket.CompletedAt.HasValue ? ticket.CompletedAt.Value.ToString("dd.MM.yyyy") : "—";
            row++;
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"Заявки_{DateTime.Now:yyyyMMdd_HHmm}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }
}
