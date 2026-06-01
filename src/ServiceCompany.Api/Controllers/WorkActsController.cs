using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Features.WorkActs.Commands;
using ServiceCompany.Application.Features.WorkActs.Queries;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/[controller]")]
public class WorkActsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;
    private readonly IFileStorageService _fileStorage;

    public WorkActsController(IMediator mediator, IApplicationDbContext context, IFileStorageService fileStorage)
    {
        _mediator = mediator;
        _context = context;
        _fileStorage = fileStorage;
    }

    [HttpGet("ticket/{ticketId:guid}")]
    public async Task<ActionResult<List<WorkActDto>>> GetByTicket(Guid ticketId, CancellationToken ct)
        => await _mediator.Send(new GetWorkActsByTicketQuery(ticketId), ct);

    [Authorize(Policy = "CanManageFinance")]
    [HttpGet("unassigned")]
    public async Task<ActionResult<List<UnassignedWorkActDto>>> GetUnassigned(
        [FromQuery] Guid? clientId, CancellationToken ct)
        => await _mediator.Send(new GetUnassignedWorkActsQuery(clientId), ct);

    [Authorize(Policy = "CanManageWorkActs")]
    [HttpPost]
    public async Task<ActionResult<Guid>> Create(CreateWorkActCommand command, CancellationToken ct)
    {
        var id = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByTicket), new { ticketId = command.TicketId }, id);
    }

    [Authorize(Policy = "CanManageWorkActs")]
    [HttpPost("{workActId:guid}/attachments")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [DisableRequestSizeLimit]
    public async Task<ActionResult<Guid>> UploadAttachment(
        Guid workActId,
        IFormFile? file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Файл не выбран. Убедитесь, что запрос отправлен как multipart/form-data.");

        var allowedExtensions = new[] { ".docx", ".xlsx", ".pdf", ".doc", ".xls" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest($"Разрешены только файлы: {string.Join(", ", allowedExtensions)}");

        var workAct = await _context.WorkActs.FindAsync(new object[] { workActId }, ct);
        if (workAct == null) return NotFound("Акт не найден.");

        await using var stream = file.OpenReadStream();
        var storagePath = await _fileStorage.UploadAsync(stream, file.FileName, ct);

        var attachment = new WorkActAttachment
        {
            WorkActId   = workActId,
            FileName    = file.FileName,
            StoragePath = storagePath,
            FileType    = file.ContentType,
            FileSize    = file.Length,
        };

        _context.WorkActAttachments.Add(attachment);
        await _context.SaveChangesAsync(ct);

        return Ok(attachment.Id);
    }

    [HttpGet("attachments/{attachmentId:guid}/download")]
    public async Task<IActionResult> DownloadAttachment(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await _context.WorkActAttachments.FindAsync(new object[] { attachmentId }, ct);
        if (attachment == null) return NotFound();

        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
        var filePath = Path.Combine(uploadsPath, attachment.StoragePath);

        if (!System.IO.File.Exists(filePath)) return NotFound("Файл не найден на сервере.");

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath, ct);
        var contentType = attachment.FileType.Length > 0
            ? attachment.FileType
            : "application/octet-stream";

        return File(fileBytes, contentType, attachment.FileName);
    }

    [Authorize(Policy = "CanManageWorkActs")]
    [HttpDelete("attachments/{attachmentId:guid}")]
    public async Task<IActionResult> DeleteAttachment(Guid attachmentId, CancellationToken ct)
    {
        var attachment = await _context.WorkActAttachments.FindAsync(new object[] { attachmentId }, ct);
        if (attachment == null) return NotFound();

        attachment.IsDeleted = true;
        await _context.SaveChangesAsync(ct);

        return NoContent();
    }
}
