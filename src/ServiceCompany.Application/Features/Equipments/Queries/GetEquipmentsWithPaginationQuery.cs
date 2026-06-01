using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using Mapster;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Equipments.Queries;

public record EquipmentDto(
    Guid Id,
    string Name,
    string? SerialNumber,
    string? Model,
    string? Manufacturer,
    DateTime? PurchaseDate,
    DateTime? WarrantyExpiryDate,
    EquipmentStatus Status,
    bool IsActive,
    Guid ServiceObjectId,
    string ServiceObjectName,
    Guid ClientId,
    string ClientName);

public class GetEquipmentsWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<EquipmentDto>>
{
    public string? SearchTerm { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? ServiceObjectId { get; set; }
}

public class GetEquipmentsWithPaginationQueryHandler : IRequestHandler<GetEquipmentsWithPaginationQuery, PaginatedResult<EquipmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetEquipmentsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<EquipmentDto>> Handle(GetEquipmentsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Equipments
            .AsNoTracking()
            .Include(e => e.ServiceObject)
                .ThenInclude(o => o!.Client)
            .OrderBy(e => e.Name)
            .AsQueryable();

        if (request.ServiceObjectId.HasValue)
        {
            query = query.Where(e => e.ServiceObjectId == request.ServiceObjectId.Value);
        }
        else if (request.ClientId.HasValue)
        {
            query = query.Where(e => e.ServiceObject != null && e.ServiceObject.ClientId == request.ClientId.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(search) || (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(search)));
        }

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(e => new EquipmentDto(
                e.Id,
                e.Name,
                e.SerialNumber,
                e.Model,
                e.Manufacturer,
                e.PurchaseDate,
                e.WarrantyExpiryDate,
                e.Status,
                e.IsActive,
                e.ServiceObjectId,
                e.ServiceObject != null ? e.ServiceObject.Name : "",
                e.ServiceObject != null ? e.ServiceObject.ClientId : Guid.Empty,
                (e.ServiceObject != null && e.ServiceObject.Client != null) ? e.ServiceObject.Client.Name : ""
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<EquipmentDto>(items, count, request.Page, request.PageSize);
    }
}
