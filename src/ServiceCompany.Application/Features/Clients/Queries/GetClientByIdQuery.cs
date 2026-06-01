using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.Clients.Queries;

public record GetClientByIdQuery(Guid Id) : IRequest<ClientDto>;

public class GetClientByIdQueryHandler : IRequestHandler<GetClientByIdQuery, ClientDto>
{
    private readonly IApplicationDbContext _context;
    public GetClientByIdQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<ClientDto> Handle(GetClientByIdQuery request, CancellationToken cancellationToken)
    {
        var client = await _context.Clients
            .AsNoTracking()
            .Include(c => c.ContactPersons)
            .Include(c => c.ServiceObjects)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (client == null)
            throw new NotFoundException(nameof(Client), request.Id);

        return new ClientDto(
            client.Id,
            client.Name,
            client.Inn,
            client.Address,
            client.Email,
            client.PhoneNumber,
            client.IsActive,
            client.ContactPersons
                .Where(cp => !cp.IsDeleted)
                .Select(cp => new ContactPersonDto(
                    cp.Id, cp.FirstName, cp.LastName,
                    cp.Position, cp.Email, cp.PhoneNumber))
                .ToList(),
            client.ServiceObjects
                .Where(o => !o.IsDeleted)
                .Select(o => new ServiceObjectRefDto(
                    o.Id, o.Name, o.Address,
                    o.Location != null ? o.Location.Y : null,
                    o.Location != null ? o.Location.X : null,
                    o.IsActive))
                .ToList());
    }
}
