using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Geo.Queries;

public record MapObjectDto(
    Guid Id,
    string Name,
    string Address,
    double Latitude,
    double Longitude,
    Guid ClientId,
    string ClientName,
    int ActiveTicketCount,
    bool HasCriticalTicket,
    bool HasOverdueTicket);

public record GetMapObjectsQuery(string? EngineerUserId = null) : IRequest<List<MapObjectDto>>;

public class GetMapObjectsQueryHandler : IRequestHandler<GetMapObjectsQuery, List<MapObjectDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public GetMapObjectsQueryHandler(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task<List<MapObjectDto>> Handle(GetMapObjectsQuery request, CancellationToken cancellationToken)
    {
        var now = _dateTimeService.UtcNow;

        var activeStatuses = new[]
        {
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.WaitingParts
        };

        var objects = await _context.ServiceObjects
            .AsNoTracking()
            .Include(o => o.Client)
            .Where(o => o.IsActive && o.Location != null)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Address,
                Latitude  = o.Location!.Y,
                Longitude = o.Location!.X,
                o.ClientId,
                ClientName = o.Client != null ? o.Client.Name : string.Empty
            })
            .ToListAsync(cancellationToken);

        if (objects.Count == 0)
            return [];

        var objectIds = objects.Select(o => o.Id).ToList();

        if (!string.IsNullOrEmpty(request.EngineerUserId))
        {
            var engineerObjectIds = await _context.Tickets
                .AsNoTracking()
                .Where(t => t.ServiceObjectId.HasValue
                            && objectIds.Contains(t.ServiceObjectId!.Value)
                            && t.AssignedUserId == request.EngineerUserId
                            && activeStatuses.Contains(t.Status))
                .Select(t => t.ServiceObjectId!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

            objects = objects.Where(o => engineerObjectIds.Contains(o.Id)).ToList();

            if (objects.Count == 0)
                return [];

            objectIds = objects.Select(o => o.Id).ToList();
        }

        var ticketsQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => t.ServiceObjectId.HasValue
                        && objectIds.Contains(t.ServiceObjectId!.Value)
                        && activeStatuses.Contains(t.Status));

        if (!string.IsNullOrEmpty(request.EngineerUserId))
            ticketsQuery = ticketsQuery.Where(t => t.AssignedUserId == request.EngineerUserId);

        var ticketStats = await ticketsQuery
            .GroupBy(t => t.ServiceObjectId!.Value)
            .Select(g => new
            {
                ObjectId     = g.Key,
                ActiveCount  = g.Count(),
                HasCritical  = g.Any(t => t.Priority == TicketPriority.Critical),
                HasOverdue   = g.Any(t => t.DueDate != null && t.DueDate < now)
            })
            .ToDictionaryAsync(x => x.ObjectId, cancellationToken);

        return objects.Select(o =>
        {
            ticketStats.TryGetValue(o.Id, out var stats);
            return new MapObjectDto(
                o.Id,
                o.Name,
                o.Address,
                o.Latitude,
                o.Longitude,
                o.ClientId,
                o.ClientName,
                stats?.ActiveCount ?? 0,
                stats?.HasCritical ?? false,
                stats?.HasOverdue   ?? false);
        }).ToList();
    }
}
