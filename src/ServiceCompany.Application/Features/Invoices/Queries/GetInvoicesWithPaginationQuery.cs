using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Invoices.Queries;

public record InvoiceDto(
    Guid Id,
    string Number,
    decimal Amount,
    InvoiceStatus Status,
    DateTime IssuedDate,
    DateTime DueDate,
    DateTime? PaidDate,
    string? Notes,
    Guid ClientId,
    string ClientName,
    List<WorkActRefDto> WorkActs);

public record WorkActRefDto(
    Guid Id,
    string Number,
    DateTime WorkDate,
    string Description,
    decimal LaborCost,
    decimal MaterialsCost,
    decimal TotalCost);

public record WorkActAttachmentRefDto(Guid Id, string FileName, string FileType, long FileSize);

public record WorkActDetailDto(
    Guid Id,
    string Number,
    DateTime WorkDate,
    string Description,
    decimal LaborCost,
    decimal MaterialsCost,
    decimal TotalCost,
    List<WorkActAttachmentRefDto> Attachments);

public class GetInvoicesWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<InvoiceDto>>
{
    public string? SearchTerm { get; set; }
    public Guid? ClientId { get; set; }
    public InvoiceStatus? Status { get; set; }
}

public class GetInvoicesWithPaginationQueryHandler
    : IRequestHandler<GetInvoicesWithPaginationQuery, PaginatedResult<InvoiceDto>>
{
    private readonly IApplicationDbContext _context;
    public GetInvoicesWithPaginationQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaginatedResult<InvoiceDto>> Handle(
        GetInvoicesWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Invoices
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.WorkActs)
            .OrderByDescending(i => i.IssuedDate)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(i => i.ClientId == request.ClientId.Value);

        if (request.Status.HasValue)
            query = query.Where(i => i.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var s = request.SearchTerm.ToLower();
            query = query.Where(i => i.Number.ToLower().Contains(s)
                                  || (i.Client != null && i.Client.Name.ToLower().Contains(s)));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(i => new InvoiceDto(
                i.Id,
                i.Number,
                i.Amount,
                i.Status,
                i.IssuedDate,
                i.DueDate,
                i.PaidDate,
                i.Notes,
                i.ClientId,
                i.Client != null ? i.Client.Name : "",
                i.WorkActs
                    .Select(w => new WorkActRefDto(
                        w.Id,
                        w.Number,
                        w.WorkDate,
                        w.Description,
                        w.LaborCost,
                        w.MaterialsCost,
                        w.TotalCost))
                    .ToList()))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<InvoiceDto>(items, count, request.Page, request.PageSize);
    }
}
