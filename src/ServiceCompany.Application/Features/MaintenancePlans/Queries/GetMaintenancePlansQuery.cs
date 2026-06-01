using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.MaintenancePlans.Queries;

public record MaintenancePlanDto(
    Guid Id,
    string Title,
    string? Description,
    string CronExpression,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive,
    Guid ServiceObjectId,
    string ServiceObjectName,
    Guid? EquipmentId,
    string? EquipmentName,
    string? DefaultEngineerId,
    TicketPriority DefaultPriority,
    DateTime? LastGeneratedDate,
    Guid? LastGeneratedTicketId,
    bool HasChecklist);

public class GetMaintenancePlansQuery : PaginatedRequest, IRequest<PaginatedResult<MaintenancePlanDto>>
{
    public string? SearchTerm { get; set; }
    public Guid? ServiceObjectId { get; set; }
    public bool? IsActive { get; set; }
}

public class GetMaintenancePlansQueryHandler
    : IRequestHandler<GetMaintenancePlansQuery, PaginatedResult<MaintenancePlanDto>>
{
    private readonly IApplicationDbContext _context;
    public GetMaintenancePlansQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaginatedResult<MaintenancePlanDto>> Handle(
        GetMaintenancePlansQuery request, CancellationToken cancellationToken)
    {
        var query = _context.MaintenancePlans
            .AsNoTracking()
            .Include(p => p.ServiceObject)
            .Include(p => p.Equipment)
            .OrderBy(p => p.Title)
            .AsQueryable();

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        if (request.ServiceObjectId.HasValue)
            query = query.Where(p => p.ServiceObjectId == request.ServiceObjectId.Value);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var s = request.SearchTerm.ToLower();
            query = query.Where(p => p.Title.ToLower().Contains(s));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new MaintenancePlanDto(
                p.Id,
                p.Title,
                p.Description,
                p.CronExpression,
                p.StartDate,
                p.EndDate,
                p.IsActive,
                p.ServiceObjectId,
                p.ServiceObject != null ? p.ServiceObject.Name : "",
                p.EquipmentId,
                p.Equipment != null ? p.Equipment.Name : null,
                p.DefaultEngineerId,
                p.DefaultPriority,
                p.LastGeneratedDate,
                p.LastGeneratedTicketId,
                p.ChecklistTemplateJson != null))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<MaintenancePlanDto>(items, count, request.Page, request.PageSize);
    }
}
