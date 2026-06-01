using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Dashboard.Queries;

public record GetDashboardStatsQuery : IRequest<DashboardStatsDto>;

public record DashboardStatsDto(
    DashboardSummaryDto Summary,
    List<ChartDataItem> StatusDistribution,
    List<ChartDataItem> PriorityDistribution,
    List<RevenueDataItem> RevenueStats,
    List<RecentActivityDto> RecentActivity);

public record DashboardSummaryDto(
    int TotalTickets,
    int ActiveTickets,
    int EmergencyTickets,
    int TotalClients,
    int SlaBreaches);

public record ChartDataItem(string Label, int Value);
public record RevenueDataItem(string Date, decimal Amount);
public record RecentActivityDto(string Action, string Description, DateTime CreatedAt, string User);

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IApplicationDbContext _context;

    public GetDashboardStatsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var totalTickets = await _context.Tickets.CountAsync(cancellationToken);
        var activeTickets = await _context.Tickets.CountAsync(t => t.Status != TicketStatus.Done && t.Status != TicketStatus.Closed, cancellationToken);
        var emergencyTickets = await _context.Tickets.CountAsync(t => t.Type == TicketType.Emergency && t.Status != TicketStatus.Closed, cancellationToken);
        var totalClients = await _context.Clients.CountAsync(cancellationToken);
        var slaBreaches = await _context.Tickets.CountAsync(t => t.IsSlaBreached, cancellationToken);

        var statusDist = await _context.Tickets
            .GroupBy(t => t.Status)
            .Select(g => new ChartDataItem(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        var priorityDist = await _context.Tickets
            .GroupBy(t => t.Priority)
            .Select(g => new ChartDataItem(g.Key.ToString(), g.Count()))
            .ToListAsync(cancellationToken);

        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
        var revenueRaw = await _context.Invoices
            .Where(i => i.Status == InvoiceStatus.Paid
                        && i.PaidDate != null
                        && i.PaidDate >= sixMonthsAgo)
            .GroupBy(i => new { i.PaidDate!.Value.Year, i.PaidDate!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(x => x.Amount) })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync(cancellationToken);

        var revenue = revenueRaw
            .Select(r => new RevenueDataItem($"{r.Year}-{r.Month:D2}", r.Amount))
            .ToList();

        var activity = await _context.AuditEntries
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .Select(a => new RecentActivityDto(a.Action, $"{a.EntityName} {a.EntityId}", a.CreatedAt, a.UserId))
            .ToListAsync(cancellationToken);

        return new DashboardStatsDto(
            new DashboardSummaryDto(totalTickets, activeTickets, emergencyTickets, totalClients, slaBreaches),
            statusDist,
            priorityDist,
            revenue,
            activity);
    }
}
