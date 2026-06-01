using ServiceCompany.Domain.Common;

namespace ServiceCompany.Domain.Entities;

public class Client : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Inn { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ContactPerson> ContactPersons { get; set; } = new List<ContactPerson>();
    public ICollection<ServiceObject> ServiceObjects { get; set; } = new List<ServiceObject>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class ContactPerson : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
}

public class Contract : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalAmount { get; set; }
    public Enums.ContractStatus Status { get; set; } = Enums.ContractStatus.Active;

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid SlaPolicyId { get; set; }
    public SlaPolicy? SlaPolicy { get; set; }
}

public class ServiceObject : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? Description { get; set; }
    public NetTopologySuite.Geometries.Point? Location { get; set; }
    public bool IsActive { get; set; } = true;

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public ICollection<Equipment> Equipments { get; set; } = new List<Equipment>();
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}

public class Equipment : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public Enums.EquipmentStatus Status { get; set; } = Enums.EquipmentStatus.Working;
    public bool IsActive { get; set; } = true;

    public Guid ServiceObjectId { get; set; }
    public ServiceObject? ServiceObject { get; set; }
}

public class Ticket : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Enums.TicketStatus Status { get; set; } = Enums.TicketStatus.New;
    public Enums.TicketPriority Priority { get; set; } = Enums.TicketPriority.Normal;
    public Enums.TicketType Type { get; set; } = Enums.TicketType.Consultation;

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid? ServiceObjectId { get; set; }
    public ServiceObject? ServiceObject { get; set; }
    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public string? AssignedUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }

    public DateTime? SlaResponseDeadline { get; set; }
    public DateTime? SlaResolutionDeadline { get; set; }
    public bool IsSlaBreached { get; set; }

    public Guid? MaintenancePlanId { get; set; }
    public MaintenancePlan? MaintenancePlan { get; set; }

    public string? ChecklistResultJson { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
    public ICollection<TicketAttachment> Attachments { get; set; } = new List<TicketAttachment>();
    public ICollection<TicketStatusHistory> StatusHistory { get; set; } = new List<TicketStatusHistory>();
    public ICollection<WorkAct> WorkActs { get; set; } = new List<WorkAct>();
}

public class TicketComment : BaseEntity
{
    public string Text { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}

public class TicketAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}

public class TicketStatusHistory : BaseEntity
{
    public Enums.TicketStatus OldStatus { get; set; }
    public Enums.TicketStatus NewStatus { get; set; }
    public string? ChangedByUserId { get; set; }
    public string? Comment { get; set; }
    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
}

public class MaintenancePlan : BaseEntity
{
    public Guid ServiceObjectId { get; set; }
    public ServiceObject? ServiceObject { get; set; }

    public Guid? EquipmentId { get; set; }
    public Equipment? Equipment { get; set; }

    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    public string CronExpression { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime? LastGeneratedDate { get; set; }
    public Guid? LastGeneratedTicketId { get; set; }

    public string? ChecklistTemplateJson { get; set; }

    public string? DefaultEngineerId { get; set; }
    public Enums.TicketPriority DefaultPriority { get; set; } = Enums.TicketPriority.Normal;
}

public class WorkAct : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public DateTime WorkDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal LaborCost { get; set; }
    public decimal MaterialsCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? PerformedByUserId { get; set; }

    public Guid TicketId { get; set; }
    public Ticket? Ticket { get; set; }
    public Guid? InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public ICollection<WorkActAttachment> Attachments { get; set; } = new List<WorkActAttachment>();
}

public class WorkActAttachment : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid WorkActId { get; set; }
    public WorkAct? WorkAct { get; set; }
}

public class Invoice : BaseEntity
{
    public string Number { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Enums.InvoiceStatus Status { get; set; } = Enums.InvoiceStatus.Draft;
    public DateTime IssuedDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? Notes { get; set; }

    public Guid ClientId { get; set; }
    public Client? Client { get; set; }
    public ICollection<WorkAct> WorkActs { get; set; } = new List<WorkAct>();
}

public class SlaPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ResponseTimeHours { get; set; }
    public int ResolutionTimeHours { get; set; }
    public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
}

public class Notification : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public bool IsRead { get; set; }
    public string? Link { get; set; }
}

public class AuditEntry : BaseEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
}
