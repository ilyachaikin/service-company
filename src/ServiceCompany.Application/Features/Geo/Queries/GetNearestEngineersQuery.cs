using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Geo.Queries;

public record NearestEngineerDto(
    string EngineerId,
    string FullName,
    string? LastKnownObjectName,
    double? DistanceKm,
    int ActiveTicketCount);

public record GetNearestEngineersQuery(Guid TargetObjectId, int Top = 5)
    : IRequest<List<NearestEngineerDto>>;

public class GetNearestEngineersQueryHandler
    : IRequestHandler<GetNearestEngineersQuery, List<NearestEngineerDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetNearestEngineersQueryHandler(
        IApplicationDbContext context,
        IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<List<NearestEngineerDto>> Handle(
        GetNearestEngineersQuery request, CancellationToken cancellationToken)
    {

        var target = await _context.ServiceObjects
            .AsNoTracking()
            .Where(o => o.Id == request.TargetObjectId && o.Location != null)
            .Select(o => new { Lat = o.Location!.Y, Lng = o.Location!.X })
            .FirstOrDefaultAsync(cancellationToken);

        if (target == null)
            throw new NotFoundException(nameof(ServiceCompany.Domain.Entities.ServiceObject), request.TargetObjectId);

        var engineers = await _identityService.GetUsersByRoleAsync("Engineer");
        if (engineers.Count == 0)
            return [];

        var engineerIds = engineers.Select(e => e.Id).ToList();

        var completedStatuses = new[] { TicketStatus.Done, TicketStatus.Closed };

        var recentTickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedUserId != null
                        && engineerIds.Contains(t.AssignedUserId)
                        && completedStatuses.Contains(t.Status)
                        && t.ServiceObjectId.HasValue)
            .Select(t => new
            {
                t.AssignedUserId,
                ServiceObjectId = t.ServiceObjectId!.Value,
                t.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var lastObjectIdByEngineer = recentTickets
            .GroupBy(t => t.AssignedUserId!)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(t => t.CompletedAt).First().ServiceObjectId);

        var lastObjectIds = lastObjectIdByEngineer.Values.Distinct().ToList();

        var lastObjects = await _context.ServiceObjects
            .AsNoTracking()
            .Where(o => lastObjectIds.Contains(o.Id) && o.Location != null)
            .Select(o => new { o.Id, o.Name, Lat = o.Location!.Y, Lng = o.Location!.X })
            .ToDictionaryAsync(o => o.Id, cancellationToken);

        var activeStatuses = new[]
        {
            TicketStatus.New, TicketStatus.Assigned,
            TicketStatus.InProgress, TicketStatus.WaitingParts
        };

        var activeCountByEngineer = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedUserId != null
                        && engineerIds.Contains(t.AssignedUserId)
                        && activeStatuses.Contains(t.Status))
            .GroupBy(t => t.AssignedUserId!)
            .Select(g => new { EngineerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.EngineerId, x => x.Count, cancellationToken);

        return engineers
            .Select(e =>
            {
                double? distanceKm = null;
                string? lastObjectName = null;

                if (lastObjectIdByEngineer.TryGetValue(e.Id, out var lastObjId)
                    && lastObjects.TryGetValue(lastObjId, out var lastObj))
                {
                    lastObjectName = lastObj.Name;
                    distanceKm = Math.Round(
                        HaversineKm(target.Lat, target.Lng, lastObj.Lat, lastObj.Lng), 1);
                }

                activeCountByEngineer.TryGetValue(e.Id, out var activeCount);

                return new NearestEngineerDto(
                    e.Id,
                    e.FullName,
                    lastObjectName,
                    distanceKm,
                    activeCount);
            })

            .OrderBy(e => e.DistanceKm.HasValue ? 0 : 1)
            .ThenBy(e => e.DistanceKm ?? double.MaxValue)
            .Take(Math.Clamp(request.Top, 1, 20))
            .ToList();
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;

        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double degrees) => degrees * Math.PI / 180.0;
}
