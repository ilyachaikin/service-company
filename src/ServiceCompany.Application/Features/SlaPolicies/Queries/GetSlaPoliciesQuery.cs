using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using Mapster;

namespace ServiceCompany.Application.Features.SlaPolicies.Queries;

public record SlaPolicyDto(Guid Id, string Name, string? Description, int ResponseTimeHours, int ResolutionTimeHours);

public record GetSlaPoliciesQuery : IRequest<List<SlaPolicyDto>>;

public class GetSlaPoliciesQueryHandler : IRequestHandler<GetSlaPoliciesQuery, List<SlaPolicyDto>>
{
    private readonly IApplicationDbContext _context;
    public GetSlaPoliciesQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<SlaPolicyDto>> Handle(GetSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        return await _context.SlaPolicies
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ProjectToType<SlaPolicyDto>()
            .ToListAsync(cancellationToken);
    }
}
