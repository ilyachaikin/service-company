using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Infrastructure.BackgroundJobs;

public class SlaMonitoringJob
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly ILogger<SlaMonitoringJob> _logger;

    public SlaMonitoringJob(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        ILogger<SlaMonitoringJob> logger)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _logger = logger;
    }

    public async Task CheckSlaBreachesAsync()
    {
        var now = _dateTimeService.UtcNow;
        var changed = false;

        var responseBreached = await _context.Tickets
            .Where(t => t.Status == TicketStatus.New
                     && !t.IsSlaBreached
                     && t.SlaResponseDeadline.HasValue
                     && t.SlaResponseDeadline < now)
            .ToListAsync();

        foreach (var ticket in responseBreached)
        {
            ticket.IsSlaBreached = true;
            _logger.LogWarning(
                "Нарушение SLA (ответ) по заявке {TicketId}: создана {CreatedAt:u}, дедлайн {Deadline:u}",
                ticket.Id, ticket.CreatedAt, ticket.SlaResponseDeadline);

            _context.TicketComments.Add(new TicketComment
            {
                TicketId  = ticket.Id,
                Text      = "⚠ Нарушение SLA по времени ответа. Зафиксировано системой мониторинга.",
                IsInternal = true,
                UserId    = "System"
            });

            changed = true;
        }

        var resolutionBreached = await _context.Tickets
            .Where(t => t.Status != TicketStatus.Done
                     && t.Status != TicketStatus.Closed
                     && t.Status != TicketStatus.Cancelled
                     && !t.IsSlaBreached
                     && t.SlaResolutionDeadline.HasValue
                     && t.SlaResolutionDeadline < now)
            .ToListAsync();

        foreach (var ticket in resolutionBreached)
        {
            ticket.IsSlaBreached = true;
            _logger.LogWarning(
                "Нарушение SLA (решение) по заявке {TicketId}: создана {CreatedAt:u}, дедлайн {Deadline:u}",
                ticket.Id, ticket.CreatedAt, ticket.SlaResolutionDeadline);

            _context.TicketComments.Add(new TicketComment
            {
                TicketId   = ticket.Id,
                Text       = "⚠ Нарушение SLA по времени решения. Зафиксировано системой мониторинга.",
                IsInternal = true,
                UserId     = "System"
            });

            changed = true;
        }

        if (changed)
        {
            await _context.SaveChangesAsync(CancellationToken.None);
            _logger.LogInformation(
                "SLA мониторинг: обновлено {ResponseCount} нарушений ответа, {ResolutionCount} нарушений решения.",
                responseBreached.Count, resolutionBreached.Count);
        }
    }
}
