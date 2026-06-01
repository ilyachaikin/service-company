using Cronos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Infrastructure.BackgroundJobs;

public class MaintenanceGenerationJob
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<MaintenanceGenerationJob> _logger;

    public MaintenanceGenerationJob(
        IApplicationDbContext context,
        IDateTimeService dateTimeService,
        INotificationService notificationService,
        ILogger<MaintenanceGenerationJob> logger)
    {
        _context = context;
        _dateTimeService = dateTimeService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = _dateTimeService.UtcNow;

        var plans = await _context.MaintenancePlans
            .AsNoTracking()
            .Include(p => p.ServiceObject)
                .ThenInclude(so => so != null ? so.Client : null)
            .Where(p => p.IsActive
                        && p.StartDate <= now
                        && (p.EndDate == null || p.EndDate >= now))
            .ToListAsync(ct);

        foreach (var plan in plans)
        {
            try
            {
                await ProcessPlanAsync(plan, now, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Ошибка при обработке плана ТО {PlanId} ({Title})",
                    plan.Id, plan.Title);
            }
        }
    }

    private async Task ProcessPlanAsync(MaintenancePlan plan, DateTime now, CancellationToken ct)
    {
        CronExpression cron;
        try
        {
            cron = CronExpression.Parse(plan.CronExpression, CronFormat.Standard);
        }
        catch (CronFormatException ex)
        {
            _logger.LogWarning(ex,
                "Неверный Cron-формат в плане ТО {PlanId}: {Cron}",
                plan.Id, plan.CronExpression);
            return;
        }

        var from = plan.LastGeneratedDate ?? plan.StartDate.AddSeconds(-1);
        var nextOccurrence = cron.GetNextOccurrence(from, TimeZoneInfo.Utc);

        if (nextOccurrence == null || nextOccurrence > now)
            return;

        var hasOpenTicket = await _context.Tickets
            .AnyAsync(t => t.MaintenancePlanId == plan.Id
                        && t.Status != TicketStatus.Done
                        && t.Status != TicketStatus.Closed
                        && t.Status != TicketStatus.Cancelled, ct);

        if (hasOpenTicket)
        {
            _logger.LogInformation(
                "ТО {PlanId}: предыдущая заявка ещё не закрыта, пропускаем генерацию.", plan.Id);

            if (!string.IsNullOrEmpty(plan.DefaultEngineerId))
            {
                await _notificationService.SendNotificationAsync(
                    plan.DefaultEngineerId,
                    $"ТО не выполнено: {plan.Title}",
                    $"Предыдущее плановое обслуживание «{plan.Title}» ещё не закрыто. " +
                    $"Новая заявка не создана.",
                    ct: ct);
            }
            return;
        }

        var ticket = new Ticket
        {
            Title          = plan.Title,
            Description    = plan.Description ?? $"Плановое ТО: {plan.Title}",
            Type           = TicketType.Scheduled,
            Priority       = plan.DefaultPriority,
            Status         = TicketStatus.New,
            ClientId       = plan.ServiceObject!.ClientId,
            ServiceObjectId = plan.ServiceObjectId,
            EquipmentId    = plan.EquipmentId,
            AssignedUserId = plan.DefaultEngineerId,
            MaintenancePlanId = plan.Id,
            ChecklistResultJson = null
        };

        if (!string.IsNullOrEmpty(plan.DefaultEngineerId))
            ticket.Status = TicketStatus.Assigned;

        _context.Tickets.Add(ticket);

        var tracked = await _context.MaintenancePlans.FindAsync(new object[] { plan.Id }, ct);
        if (tracked != null)
        {
            tracked.LastGeneratedDate    = now;
            tracked.LastGeneratedTicketId = ticket.Id;
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "ТО {PlanId}: создана заявка {TicketId} для объекта {ObjectId}",
            plan.Id, ticket.Id, plan.ServiceObjectId);
    }
}
