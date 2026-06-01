using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Tickets.Queries;

public record TicketDto(
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
    string? AssignedUserName,
    DateTime CreatedAt,
    DateTime? DueDate,
    DateTime? CompletedAt);

public class GetTicketsWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<TicketDto>>
{
    public string? SearchTerm { get; set; }
    public TicketStatus? Status { get; set; }
    public TicketPriority? Priority { get; set; }
    public Guid? ClientId { get; set; }
    public string? AssignedUserId { get; set; }
}

public class GetTicketsWithPaginationQueryHandler : IRequestHandler<GetTicketsWithPaginationQuery, PaginatedResult<TicketDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentityService _identityService;

    public GetTicketsWithPaginationQueryHandler(IApplicationDbContext context, IIdentityService identityService)
    {
        _context = context;
        _identityService = identityService;
    }

    public async Task<PaginatedResult<TicketDto>> Handle(GetTicketsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Tickets
            .AsNoTracking()
            .Include(t => t.Client)
            .Include(t => t.ServiceObject)
            .Include(t => t.Equipment)
            .OrderByDescending(t => t.CreatedAt)
            .AsQueryable();

        if (request.Status.HasValue) query = query.Where(t => t.Status == request.Status.Value);
        if (request.Priority.HasValue) query = query.Where(t => t.Priority == request.Priority.Value);
        if (request.ClientId.HasValue) query = query.Where(t => t.ClientId == request.ClientId.Value);
        if (!string.IsNullOrEmpty(request.AssignedUserId)) query = query.Where(t => t.AssignedUserId == request.AssignedUserId);

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(search) || t.Description.ToLower().Contains(search));
        }

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.Id,
                t.Title,
                t.Description,
                t.Status,
                t.Priority,
                t.Type,
                t.ClientId,
                ClientName = t.Client != null ? t.Client.Name : "",
                t.ServiceObjectId,
                ServiceObjectName = t.ServiceObject != null ? t.ServiceObject.Name : null,
                t.EquipmentId,
                EquipmentName = t.Equipment != null ? t.Equipment.Name : null,
                t.AssignedUserId,
                t.CreatedAt,
                t.DueDate,
                t.CompletedAt
            })
            .ToListAsync(cancellationToken);

        var assignedUserIds = items
            .Where(t => !string.IsNullOrEmpty(t.AssignedUserId))
            .Select(t => t.AssignedUserId!)
            .Distinct()
            .ToHashSet();

        Dictionary<string, string> userNames = [];
        if (assignedUserIds.Count > 0)
        {
            var allUsers = await _identityService.GetAllUsersAsync();
            userNames = allUsers
                .Where(u => assignedUserIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.FullName);
        }

        var dtos = items.Select(t => new TicketDto(
            t.Id,
            t.Title,
            t.Description,
            t.Status,
            t.Priority,
            t.Type,
            t.ClientId,
            t.ClientName,
            t.ServiceObjectId,
            t.ServiceObjectName,
            t.EquipmentId,
            t.EquipmentName,
            t.AssignedUserId,
            t.AssignedUserId != null && userNames.TryGetValue(t.AssignedUserId, out var name) ? name : null,
            t.CreatedAt,
            t.DueDate,
            t.CompletedAt
        )).ToList();

        return new PaginatedResult<TicketDto>(dtos, count, request.Page, request.PageSize);
    }
}
