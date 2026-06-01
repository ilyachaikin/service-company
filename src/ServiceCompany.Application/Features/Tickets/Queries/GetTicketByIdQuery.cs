using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Tickets.Queries;

public record TicketDetailsDto(
    Guid Id,
    string Title,
    string Description,
    TicketStatus Status,
    TicketPriority Priority,
    TicketType Type,
    Guid ClientId,
    string ClientName,
    Guid? ServiceObjectId,
    string? ServiceObjectName,
    Guid? EquipmentId,
    string? EquipmentName,
    string? AssignedUserId,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? CompletedAt,
    List<TicketCommentDto> Comments,
    List<TicketHistoryDto> History);

public record TicketCommentDto(
    Guid Id,
    string Text,
    string UserId,
    string UserName,
    bool IsInternal,
    DateTime CreatedAt);

public record TicketHistoryDto(
    Guid Id,
    TicketStatus OldStatus,
    TicketStatus NewStatus,
    string? ChangedByUserId,
    string? Comment,
    DateTime ChangedAt);

public record GetTicketByIdQuery(Guid Id) : IRequest<TicketDetailsDto>;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDetailsDto>
{
    private readonly IApplicationDbContext _context;

    public GetTicketByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketDetailsDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _context.Tickets
            .AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.ServiceObject)
            .Include(t => t.Equipment)
            .Include(t => t.Comments.OrderByDescending(c => c.CreatedAt))
            .Include(t => t.StatusHistory.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null) throw new NotFoundException(nameof(Ticket), request.Id);

        return new TicketDetailsDto(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Status,
            ticket.Priority,
            ticket.Type,
            ticket.ClientId,
            ticket.Client?.Name ?? "",
            ticket.ServiceObjectId,
            ticket.ServiceObject?.Name,
            ticket.EquipmentId,
            ticket.Equipment?.Name,
            ticket.AssignedUserId,
            ticket.CreatedAt,
            ticket.DueDate,
            ticket.CompletedAt,
            ticket.Comments.Select(c => new TicketCommentDto(c.Id, c.Text, c.UserId, "", c.IsInternal, c.CreatedAt)).ToList(),
            ticket.StatusHistory.Select(h => new TicketHistoryDto(h.Id, h.OldStatus, h.NewStatus, h.ChangedByUserId, h.Comment, h.CreatedAt)).ToList()
        );
    }
}
