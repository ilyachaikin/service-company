using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;

namespace ServiceCompany.Application.Features.WorkActs.Queries;

public record WorkActAttachmentDto(Guid Id, string FileName, string FileType, long FileSize);

public record WorkActDto(
    Guid Id,
    string Number,
    DateTime WorkDate,
    string Description,
    decimal LaborCost,
    decimal MaterialsCost,
    decimal TotalCost,
    string? PerformedByUserId,
    Guid? InvoiceId,
    string? InvoiceNumber,
    List<WorkActAttachmentDto> Attachments);

public record GetWorkActsByTicketQuery(Guid TicketId) : IRequest<List<WorkActDto>>;

public class GetWorkActsByTicketQueryHandler : IRequestHandler<GetWorkActsByTicketQuery, List<WorkActDto>>
{
    private readonly IApplicationDbContext _context;
    public GetWorkActsByTicketQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<WorkActDto>> Handle(GetWorkActsByTicketQuery request, CancellationToken cancellationToken)
    {

        var acts = await _context.WorkActs
            .AsNoTracking()
            .Include(w => w.Invoice)
            .Where(w => w.TicketId == request.TicketId)
            .OrderByDescending(w => w.WorkDate)
            .ToListAsync(cancellationToken);

        if (acts.Count == 0)
            return [];

        Dictionary<Guid, List<WorkActAttachmentDto>> attachmentMap = [];
        try
        {
            var actIds = acts.Select(w => w.Id).ToList();
            var attachments = await _context.WorkActAttachments
                .AsNoTracking()
                .Where(a => actIds.Contains(a.WorkActId))
                .Select(a => new { a.Id, a.WorkActId, a.FileName, a.FileType, a.FileSize })
                .ToListAsync(cancellationToken);

            attachmentMap = attachments
                .GroupBy(a => a.WorkActId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(a => new WorkActAttachmentDto(a.Id, a.FileName, a.FileType, a.FileSize)).ToList());
        }
        catch
        {

        }

        return acts.Select(w => new WorkActDto(
            w.Id,
            w.Number,
            w.WorkDate,
            w.Description,
            w.LaborCost,
            w.MaterialsCost,
            w.TotalCost,
            w.PerformedByUserId,
            w.InvoiceId,
            w.Invoice?.Number,
            attachmentMap.TryGetValue(w.Id, out var atts) ? atts : []
        )).ToList();
    }
}
