using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Application.Features.Clients.Queries;

namespace ServiceCompany.Application.Features.ContactPersons.Queries;

public record GetContactPersonsByClientQuery(Guid ClientId) : IRequest<List<ContactPersonDto>>;

public class GetContactPersonsByClientQueryHandler
    : IRequestHandler<GetContactPersonsByClientQuery, List<ContactPersonDto>>
{
    private readonly IApplicationDbContext _context;
    public GetContactPersonsByClientQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<List<ContactPersonDto>> Handle(
        GetContactPersonsByClientQuery request, CancellationToken cancellationToken)
    {
        return await _context.ContactPersons
            .AsNoTracking()
            .Where(cp => cp.ClientId == request.ClientId)
            .OrderBy(cp => cp.LastName).ThenBy(cp => cp.FirstName)
            .Select(cp => new ContactPersonDto(
                cp.Id, cp.FirstName, cp.LastName,
                cp.Position, cp.Email, cp.PhoneNumber))
            .ToListAsync(cancellationToken);
    }
}
