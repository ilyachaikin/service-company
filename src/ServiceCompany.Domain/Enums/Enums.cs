namespace ServiceCompany.Domain.Enums;

public enum TicketStatus { New, Assigned, InProgress, WaitingParts, Done, Closed, Cancelled, Archived }
public enum TicketPriority { Critical, High, Normal, Low }
public enum TicketType { Emergency, Scheduled, Consultation }
public enum EquipmentStatus { Working, Broken, Maintenance, Decommissioned }
public enum ContractStatus { Draft, Active, Suspended, Expired, Terminated }
public enum InvoiceStatus { Draft, Sent, Paid, Overdue, Cancelled }
public enum PaymentStatus { Unpaid, PartiallyPaid, Paid }
