using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _dateTimeService;

    public AuditService(IApplicationDbContext context, IDateTimeService dateTimeService)
    {
        _context = context;
        _dateTimeService = dateTimeService;
    }

    public async Task LogAsync(string userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null)
    {
        var auditEntry = new AuditEntry
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = _dateTimeService.UtcNow
        };

        _context.AuditEntries.Add(auditEntry);
        await _context.SaveChangesAsync(default);
    }
}
