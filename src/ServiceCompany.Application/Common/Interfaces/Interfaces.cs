using ServiceCompany.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ServiceCompany.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Client> Clients { get; }
    DbSet<ContactPerson> ContactPersons { get; }
    DbSet<Contract> Contracts { get; }
    DbSet<ServiceObject> ServiceObjects { get; }
    DbSet<Equipment> Equipments { get; }
    DbSet<Ticket> Tickets { get; }
    DbSet<TicketComment> TicketComments { get; }
    DbSet<TicketAttachment> TicketAttachments { get; }
    DbSet<TicketStatusHistory> TicketStatusHistory { get; }
    DbSet<MaintenancePlan> MaintenancePlans { get; }
    DbSet<WorkAct> WorkActs { get; }
    DbSet<WorkActAttachment> WorkActAttachments { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<SlaPolicy> SlaPolicies { get; }
    DbSet<AuditEntry> AuditEntries { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct);
}

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream file, string fileName, CancellationToken ct);
}

public interface IDateTimeService
{
    DateTime UtcNow { get; }
}
