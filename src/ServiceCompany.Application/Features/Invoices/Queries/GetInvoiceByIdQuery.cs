using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.Invoices.Queries;

public record InvoiceDetailDto(
    Guid Id,
    string Number,
    decimal Amount,
    int Status,
    DateTime IssuedDate,
    DateTime DueDate,
    DateTime? PaidDate,
    string? Notes,
    Guid ClientId,
    string ClientName,
    List<WorkActDetailDto> WorkActs);

public record GetInvoiceByIdQuery(Guid Id) : IRequest<InvoiceDetailDto>;

public class GetInvoiceByIdQueryHandler : IRequestHandler<GetInvoiceByIdQuery, InvoiceDetailDto>
{
    private readonly IApplicationDbContext _context;
    public GetInvoiceByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<InvoiceDetailDto> Handle(GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.Client)
            .Include(i => i.WorkActs)
                .ThenInclude(w => w.Attachments)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);

        if (invoice == null)
            throw new NotFoundException(nameof(Invoice), request.Id);

        return new InvoiceDetailDto(
            invoice.Id,
            invoice.Number,
            invoice.Amount,
            (int)invoice.Status,
            invoice.IssuedDate,
            invoice.DueDate,
            invoice.PaidDate,
            invoice.Notes,
            invoice.ClientId,
            invoice.Client?.Name ?? "",
            invoice.WorkActs
                .Select(w => new WorkActDetailDto(
                    w.Id,
                    w.Number,
                    w.WorkDate,
                    w.Description,
                    w.LaborCost,
                    w.MaterialsCost,
                    w.TotalCost,
                    w.Attachments
                        .Where(a => !a.IsDeleted)
                        .Select(a => new WorkActAttachmentRefDto(a.Id, a.FileName, a.FileType, a.FileSize))
                        .ToList()))
                .ToList());
    }
}
