using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using Mapster;

namespace ServiceCompany.Application.Features.ServiceObjects.Queries;

public record ServiceObjectDto(
    Guid Id,
    string Name,
    string Address,
    string? Description,
    double? Latitude,
    double? Longitude,
    Guid ClientId,
    string ClientName,
    bool IsActive);

public class GetServiceObjectsWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<ServiceObjectDto>>
{
    public string? SearchTerm { get; set; }
    public Guid? ClientId { get; set; }
}

public class GetServiceObjectsWithPaginationQueryHandler : IRequestHandler<GetServiceObjectsWithPaginationQuery, PaginatedResult<ServiceObjectDto>>
{
    private readonly IApplicationDbContext _context;

    public GetServiceObjectsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ServiceObjectDto>> Handle(GetServiceObjectsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ServiceObjects
            .AsNoTracking()
            .Include(o => o.Client)
            .OrderBy(o => o.Name)
            .AsQueryable();

        if (request.ClientId.HasValue)
        {
            query = query.Where(o => o.ClientId == request.ClientId.Value);
        }

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(o => o.Name.ToLower().Contains(search) || o.Address.ToLower().Contains(search));
        }

        var count = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(o => new ServiceObjectDto(
                o.Id,
                o.Name,
                o.Address,
                o.Description,
                o.Location != null ? o.Location.Y : null,
                o.Location != null ? o.Location.X : null,
                o.ClientId,
                o.Client != null ? o.Client.Name : "",
                o.IsActive
            ))
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ServiceObjectDto>(items, count, request.Page, request.PageSize);
    }
}
