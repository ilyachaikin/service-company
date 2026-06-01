using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Reports.Queries;

public record SlaReportDto(
    double SlaResponseRatePct,
    double SlaResolutionRatePct,
    double MttrHours,
    int TotalClosed,
    int SlaBreached);

public record EngineerWorkloadDto(
    string EngineerId,
    string EngineerName,
    int ActiveTickets,
    int ClosedThisPeriod,
    double AvgResolutionHours);

public record ProblematicEquipmentDto(
    Guid EquipmentId,
    string EquipmentName,
    string? SerialNumber,
    string ServiceObjectName,
    int TicketCount);

public record TicketsByObjectDto(
    Guid ServiceObjectId,
    string ServiceObjectName,
    string ClientName,
    int TotalTickets,
    int OpenTickets);

public record AvgResolutionByPriorityDto(
    TicketPriority Priority,
    double AvgHours,
    int Count);

public record GetSlaReportQuery(DateTime From, DateTime To, Guid? ClientId) : IRequest<SlaReportDto>;

public class GetSlaReportQueryHandler : IRequestHandler<GetSlaReportQuery, SlaReportDto>
{
    private readonly IApplicationDbContext _context;
    public GetSlaReportQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<SlaReportDto> Handle(GetSlaReportQuery request, CancellationToken cancellationToken)
    {
        var closedStatuses = new[] { TicketStatus.Done, TicketStatus.Closed };

        var ticketsQuery = _context.Tickets
            .AsNoTracking()
            .Where(t => t.CreatedAt >= request.From && t.CreatedAt <= request.To);

        if (request.ClientId.HasValue)
            ticketsQuery = ticketsQuery.Where(t => t.ClientId == request.ClientId.Value);

        var tickets = await ticketsQuery
            .Select(t => new
            {
                t.SlaResponseDeadline,
                t.SlaResolutionDeadline,
                t.IsSlaBreached,
                t.Status,
                t.CreatedAt,
                t.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var closed = tickets.Where(t => closedStatuses.Contains(t.Status)).ToList();
        var total  = tickets.Count;

        var withResponseDeadline = tickets.Where(t => t.SlaResponseDeadline.HasValue).ToList();
        var responseRate = withResponseDeadline.Count == 0 ? 100.0
            : withResponseDeadline.Count(t => !t.IsSlaBreached) * 100.0 / withResponseDeadline.Count;

        var closedWithDeadline = closed.Where(t => t.SlaResolutionDeadline.HasValue && t.CompletedAt.HasValue).ToList();
        var resolutionRate = closedWithDeadline.Count == 0 ? 100.0
            : closedWithDeadline.Count(t => t.CompletedAt <= t.SlaResolutionDeadline) * 100.0
              / closedWithDeadline.Count;

        var mttr = closed.Where(t => t.CompletedAt.HasValue).ToList();
        var mttrHours = mttr.Count == 0 ? 0.0
            : mttr.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours);

        return new SlaReportDto(
            Math.Round(responseRate, 1),
            Math.Round(resolutionRate, 1),
            Math.Round(mttrHours, 1),
            closed.Count,
            tickets.Count(t => t.IsSlaBreached));
    }
}

public record GetEngineerWorkloadQuery(DateTime From, DateTime To) : IRequest<List<EngineerWorkloadDto>>;

public class GetEngineerWorkloadQueryHandler : IRequestHandler<GetEngineerWorkloadQuery, List<EngineerWorkloadDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetEngineerWorkloadQueryHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<List<EngineerWorkloadDto>> Handle(
        GetEngineerWorkloadQuery request, CancellationToken cancellationToken)
    {
        var engineers = await _identityService.GetUsersByRoleAsync("Engineer");
        if (engineers.Count == 0) return [];

        var engineerIds = engineers.Select(e => e.Id).ToList();
        var activeStatuses = new[] { TicketStatus.New, TicketStatus.Assigned, TicketStatus.InProgress, TicketStatus.WaitingParts };
        var closedStatuses = new[] { TicketStatus.Done, TicketStatus.Closed };

        var tickets = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.AssignedUserId != null && engineerIds.Contains(t.AssignedUserId))
            .Select(t => new
            {
                t.AssignedUserId,
                t.Status,
                t.CreatedAt,
                t.CompletedAt
            })
            .ToListAsync(cancellationToken);

        return engineers.Select(e =>
        {
            var mine = tickets.Where(t => t.AssignedUserId == e.Id).ToList();
            var active = mine.Count(t => activeStatuses.Contains(t.Status));
            var closed = mine.Where(t => closedStatuses.Contains(t.Status)
                                      && t.CompletedAt.HasValue
                                      && t.CompletedAt >= request.From
                                      && t.CompletedAt <= request.To).ToList();
            var avgHours = closed.Count == 0 ? 0.0
                : closed.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours);

            return new EngineerWorkloadDto(e.Id, e.FullName, active, closed.Count, Math.Round(avgHours, 1));
        })
        .OrderByDescending(e => e.ActiveTickets)
        .ToList();
    }
}

public record GetProblematicEquipmentQuery(DateTime From, DateTime To, int Top = 10)
    : IRequest<List<ProblematicEquipmentDto>>;

public class GetProblematicEquipmentQueryHandler
    : IRequestHandler<GetProblematicEquipmentQuery, List<ProblematicEquipmentDto>>
{
    private readonly IApplicationDbContext _context;
    public GetProblematicEquipmentQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<ProblematicEquipmentDto>> Handle(
        GetProblematicEquipmentQuery request, CancellationToken cancellationToken)
    {
        var grouped = await _context.Tickets
            .AsNoTracking()
            .Where(t => t.EquipmentId.HasValue && t.CreatedAt >= request.From && t.CreatedAt <= request.To)
            .GroupBy(t => t.EquipmentId!.Value)
            .Select(g => new { EquipmentId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(request.Top)
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0) return [];

        var equipmentIds = grouped.Select(g => g.EquipmentId).ToList();
        var equipments = await _context.Equipments
            .AsNoTracking()
            .Include(e => e.ServiceObject)
            .Where(e => equipmentIds.Contains(e.Id))
            .ToListAsync(cancellationToken);

        var equipDict = equipments.ToDictionary(e => e.Id);

        return grouped
            .Where(g => equipDict.ContainsKey(g.EquipmentId))
            .Select(g =>
            {
                var e = equipDict[g.EquipmentId];
                return new ProblematicEquipmentDto(
                    e.Id, e.Name, e.SerialNumber,
                    e.ServiceObject?.Name ?? "", g.Count);
            })
            .ToList();
    }
}

public record GetTicketsByObjectQuery(DateTime From, DateTime To, Guid? ClientId)
    : IRequest<List<TicketsByObjectDto>>;

public class GetTicketsByObjectQueryHandler
    : IRequestHandler<GetTicketsByObjectQuery, List<TicketsByObjectDto>>
{
    private readonly IApplicationDbContext _context;
    public GetTicketsByObjectQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<TicketsByObjectDto>> Handle(
        GetTicketsByObjectQuery request, CancellationToken cancellationToken)
    {
        var activeStatuses = new[]
        {
            TicketStatus.New, TicketStatus.Assigned,
            TicketStatus.InProgress, TicketStatus.WaitingParts
        };

        var q = _context.Tickets
            .AsNoTracking()
            .Where(t => t.ServiceObjectId.HasValue
                     && t.CreatedAt >= request.From
                     && t.CreatedAt <= request.To);

        if (request.ClientId.HasValue)
            q = q.Where(t => t.ClientId == request.ClientId.Value);

        var grouped = await q
            .GroupBy(t => t.ServiceObjectId!.Value)
            .Select(g => new
            {
                ObjectId     = g.Key,
                TotalTickets = g.Count(),
                OpenTickets  = g.Count(t => activeStatuses.Contains(t.Status))
            })
            .ToListAsync(cancellationToken);

        if (grouped.Count == 0) return [];

        var objectIds = grouped.Select(g => g.ObjectId).ToList();
        var serviceObjects = await _context.ServiceObjects
            .AsNoTracking()
            .Include(o => o.Client)
            .Where(o => objectIds.Contains(o.Id))
            .ToListAsync(cancellationToken);

        var objectDict = serviceObjects.ToDictionary(o => o.Id);

        return grouped
            .Where(g => objectDict.ContainsKey(g.ObjectId))
            .Select(g =>
            {
                var o = objectDict[g.ObjectId];
                return new TicketsByObjectDto(
                    o.Id,
                    o.Name,
                    o.Client?.Name ?? "",
                    g.TotalTickets,
                    g.OpenTickets);
            })
            .OrderByDescending(x => x.TotalTickets)
            .ToList();
    }
}

public record GetAvgResolutionByPriorityQuery(DateTime From, DateTime To)
    : IRequest<List<AvgResolutionByPriorityDto>>;

public class GetAvgResolutionByPriorityQueryHandler
    : IRequestHandler<GetAvgResolutionByPriorityQuery, List<AvgResolutionByPriorityDto>>
{
    private readonly IApplicationDbContext _context;
    public GetAvgResolutionByPriorityQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<AvgResolutionByPriorityDto>> Handle(
        GetAvgResolutionByPriorityQuery request, CancellationToken cancellationToken)
    {
        var closedStatuses = new[] { TicketStatus.Done, TicketStatus.Closed };

        var raw = await _context.Tickets
            .AsNoTracking()
            .Where(t => closedStatuses.Contains(t.Status)
                     && t.CompletedAt.HasValue
                     && t.CreatedAt >= request.From
                     && t.CreatedAt <= request.To)
            .Select(t => new { t.Priority, t.CreatedAt, t.CompletedAt })
            .ToListAsync(cancellationToken);

        return raw
            .GroupBy(t => t.Priority)
            .Select(g => new AvgResolutionByPriorityDto(
                g.Key,
                Math.Round(g.Average(t => (t.CompletedAt!.Value - t.CreatedAt).TotalHours), 1),
                g.Count()))
            .OrderBy(x => x.Priority)
            .ToList();
    }
}
