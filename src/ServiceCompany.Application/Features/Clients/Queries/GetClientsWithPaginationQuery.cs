using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;

namespace ServiceCompany.Application.Features.Clients.Queries;

public record ServiceObjectRefDto(
    Guid Id,
    string Name,
    string? Address,
    double? Latitude,
    double? Longitude,
    bool IsActive);

public record ClientDto(
    Guid Id,
    string Name,
    string Inn,
    string? Address,
    string? Email,
    string? PhoneNumber,
    bool IsActive,
    List<ContactPersonDto> ContactPersons,
    List<ServiceObjectRefDto> ServiceObjects);

public record ContactPersonDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? Position,
    string? Email,
    string? PhoneNumber);

public class GetClientsWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<ClientDto>>
{
    public string? SearchTerm { get; set; }
}

public class GetClientsWithPaginationQueryHandler : IRequestHandler<GetClientsWithPaginationQuery, PaginatedResult<ClientDto>>
{
    private readonly IApplicationDbContext _context;

    public GetClientsWithPaginationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<ClientDto>> Handle(GetClientsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Clients
            .AsNoTracking()
            .Include(c => c.ContactPersons)
            .OrderBy(c => c.Name)
            .AsQueryable();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search) || c.Inn.Contains(search));
        }

        var count = await query.CountAsync(cancellationToken);

        var rawItems = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = rawItems.Select(c => new ClientDto(
            c.Id, c.Name, c.Inn, c.Address, c.Email, c.PhoneNumber, c.IsActive,
            c.ContactPersons.Where(cp => !cp.IsDeleted)
                .Select(cp => new ContactPersonDto(cp.Id, cp.FirstName, cp.LastName, cp.Position, cp.Email, cp.PhoneNumber))
                .ToList(),
            [])).ToList();

        return new PaginatedResult<ClientDto>(items, count, request.Page, request.PageSize);
    }
}
