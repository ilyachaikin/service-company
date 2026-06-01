namespace ServiceCompany.Application.Common.Interfaces;

public interface IAuditService
{
    Task LogAsync(string userId, string action, string entityName, string entityId, string? oldValues = null, string? newValues = null);
}
