using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Infrastructure.Identity;
using ServiceCompany.Infrastructure.Persistence.Interceptors;

namespace ServiceCompany.Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly IMediator _mediator;
    private readonly AuditableEntityInterceptor _auditableEntityInterceptor;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        IMediator mediator,
        AuditableEntityInterceptor auditableEntityInterceptor) : base(options)
    {
        _mediator = mediator;
        _auditableEntityInterceptor = auditableEntityInterceptor;
    }

    public DbSet<Client> Clients => Set<Client>();
    public DbSet<ContactPerson> ContactPersons => Set<ContactPerson>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ServiceObject> ServiceObjects => Set<ServiceObject>();
    public DbSet<Equipment> Equipments => Set<Equipment>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketComment> TicketComments => Set<TicketComment>();
    public DbSet<TicketAttachment> TicketAttachments => Set<TicketAttachment>();
    public DbSet<TicketStatusHistory> TicketStatusHistory => Set<TicketStatusHistory>();
    public DbSet<MaintenancePlan> MaintenancePlans => Set<MaintenancePlan>();
    public DbSet<WorkAct> WorkActs => Set<WorkAct>();
    public DbSet<WorkActAttachment> WorkActAttachments => Set<WorkActAttachment>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<SlaPolicy> SlaPolicies => Set<SlaPolicy>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Client>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ContactPerson>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Contract>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<ServiceObject>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Equipment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Ticket>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<TicketComment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<TicketAttachment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<TicketStatusHistory>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Notification>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<SlaPolicy>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<WorkAct>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<WorkActAttachment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Invoice>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<MaintenancePlan>().HasQueryFilter(x => !x.IsDeleted);

        builder.Entity<Client>(entity =>
        {
            entity.HasIndex(e => e.Inn).IsUnique();
            entity.HasMany(e => e.ContactPersons)
                  .WithOne(e => e.Client)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ServiceObjects)
                  .WithOne(e => e.Client)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ServiceObject>(entity =>
        {
            entity.HasMany(e => e.Equipments)
                  .WithOne(e => e.ServiceObject)
                  .HasForeignKey(e => e.ServiceObjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Location)
                  .HasMethod("gist")
                  .HasDatabaseName("IX_ServiceObjects_Location");
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.HasMany(e => e.Comments)
                  .WithOne(e => e.Ticket)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Attachments)
                  .WithOne(e => e.Ticket)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.StatusHistory)
                  .WithOne(e => e.Ticket)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Contract>(entity =>
        {
            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Contracts)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SlaPolicy)
                  .WithMany(s => s.Contracts)
                  .HasForeignKey(e => e.SlaPolicyId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasOne(e => e.Client)
                  .WithMany(c => c.Invoices)
                  .HasForeignKey(e => e.ClientId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.WorkActs)
                  .WithOne(e => e.Invoice)
                  .HasForeignKey(e => e.InvoiceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<WorkAct>(entity =>
        {
            entity.HasOne(e => e.Ticket)
                  .WithMany(t => t.WorkActs)
                  .HasForeignKey(e => e.TicketId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Attachments)
                  .WithOne(e => e.WorkAct)
                  .HasForeignKey(e => e.WorkActId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MaintenancePlan>(entity =>
        {
            entity.HasOne(e => e.ServiceObject)
                  .WithMany()
                  .HasForeignKey(e => e.ServiceObjectId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Equipment)
                  .WithMany()
                  .HasForeignKey(e => e.EquipmentId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Ticket>(entity =>
        {
            entity.HasOne(e => e.MaintenancePlan)
                  .WithMany()
                  .HasForeignKey(e => e.MaintenancePlanId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Ticket>()
            .HasIndex(t => new { t.Status, t.CreatedAt })
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Tickets_Status_CreatedAt");

        builder.Entity<Ticket>()
            .HasIndex(t => t.AssignedUserId)
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Tickets_AssignedUserId");

        builder.Entity<Ticket>()
            .HasIndex(t => t.ServiceObjectId)
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Tickets_ServiceObjectId");

        builder.Entity<Ticket>()
            .HasIndex(t => t.EquipmentId)
            .HasFilter("\"IsDeleted\" = false")
            .HasDatabaseName("IX_Tickets_EquipmentId");

        builder.HasPostgresExtension("postgis");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditableEntityInterceptor);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {

        var entitiesWithEvents = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
        {
            var events = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();
            foreach (var domainEvent in events)
                await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}
