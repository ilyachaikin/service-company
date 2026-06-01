using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;

namespace ServiceCompany.Application.Features.WorkActs.Queries;

public record GetUnassignedWorkActsQuery(Guid? ClientId = null) : IRequest<List<UnassignedWorkActDto>>;

public record UnassignedWorkActDto(
    Guid Id,
    string Number,
    DateTime WorkDate,
    string Description,
    decimal TotalCost,
    Guid TicketId,
    string? TicketTitle,
    Guid ClientId,
    string ClientName);

public class GetUnassignedWorkActsQueryHandler : IRequestHandler<GetUnassignedWorkActsQuery, List<UnassignedWorkActDto>>
{
    private readonly IApplicationDbContext _context;
    public GetUnassignedWorkActsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<UnassignedWorkActDto>> Handle(GetUnassignedWorkActsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.WorkActs
            .AsNoTracking()
            .Include(w => w.Ticket)
                .ThenInclude(t => t!.Client)
            .Where(w => w.InvoiceId == null && w.Ticket != null)
            .AsQueryable();

        if (request.ClientId.HasValue)
            query = query.Where(w => w.Ticket!.ClientId == request.ClientId.Value);

        var acts = await query
            .OrderByDescending(w => w.WorkDate)
            .ToListAsync(cancellationToken);

        return acts.Select(w => new UnassignedWorkActDto(
            w.Id,
            w.Number,
            w.WorkDate,
            w.Description,
            w.TotalCost,
            w.TicketId,
            w.Ticket?.Title,
            w.Ticket!.ClientId,
            w.Ticket.Client?.Name ?? ""
        )).ToList();
    }
}
