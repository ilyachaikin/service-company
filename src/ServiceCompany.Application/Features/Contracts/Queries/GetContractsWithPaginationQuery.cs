using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Common.Models;
using ServiceCompany.Domain.Enums;
using Mapster;

namespace ServiceCompany.Application.Features.Contracts.Queries;

public record ContractDto(
    Guid Id,
    string Number,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalAmount,
    ContractStatus Status,
    Guid ClientId,
    string ClientName,
    Guid SlaPolicyId,
    string SlaPolicyName);

public class GetContractsWithPaginationQuery : PaginatedRequest, IRequest<PaginatedResult<ContractDto>>
{
    public string? SearchTerm { get; set; }
    public Guid? ClientId { get; set; }
}

public class GetContractsWithPaginationQueryHandler : IRequestHandler<GetContractsWithPaginationQuery, PaginatedResult<ContractDto>>
{
    private readonly IApplicationDbContext _context;
    public GetContractsWithPaginationQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<PaginatedResult<ContractDto>> Handle(GetContractsWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Contracts
            .AsNoTracking()
            .Include(c => c.Client)
            .Include(c => c.SlaPolicy)
            .OrderByDescending(c => c.StartDate)
            .AsQueryable();

        if (request.ClientId.HasValue) query = query.Where(c => c.ClientId == request.ClientId.Value);
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(c => c.Number.ToLower().Contains(search) || (c.Client != null && c.Client.Name.ToLower().Contains(search)));
        }

        var count = await query.CountAsync(cancellationToken);
        var items = await query
            .ProjectToType<ContractDto>()
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<ContractDto>(items, count, request.Page, request.PageSize);
    }
}
