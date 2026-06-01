using FluentValidation;
using MediatR;
using NetTopologySuite.Geometries;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.ServiceObjects.Commands;

public record CreateServiceObjectCommand(
    string Name,
    string Address,
    string? Description,
    double? Latitude,
    double? Longitude,
    Guid ClientId) : IRequest<Guid>;

public class CreateServiceObjectCommandValidator : AbstractValidator<CreateServiceObjectCommand>
{
    public CreateServiceObjectCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование объекта обязательно.")
            .MaximumLength(200).WithMessage("Наименование не может превышать 200 символов.");

        RuleFor(v => v.Address)
            .NotEmpty().WithMessage("Адрес объекта обязателен.")
            .MaximumLength(500).WithMessage("Адрес не может превышать 500 символов.");

        RuleFor(v => v.ClientId)
            .NotEmpty().WithMessage("Необходимо указать клиента.");

        RuleFor(v => v.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Широта должна быть в диапазоне от -90 до 90.")
            .When(v => v.Latitude.HasValue);

        RuleFor(v => v.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Долгота должна быть в диапазоне от -180 до 180.")
            .When(v => v.Longitude.HasValue);
    }
}

public class CreateServiceObjectCommandHandler : IRequestHandler<CreateServiceObjectCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public CreateServiceObjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateServiceObjectCommand request, CancellationToken cancellationToken)
    {
        var entity = new ServiceObject
        {
            Name        = request.Name,
            Address     = request.Address,
            Description = request.Description,
            ClientId    = request.ClientId,
            IsActive    = true
        };

        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            entity.Location = _geometryFactory.CreatePoint(
                new Coordinate(request.Longitude.Value, request.Latitude.Value));
        }

        _context.ServiceObjects.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateServiceObjectCommand(
    Guid Id,
    string Name,
    string Address,
    string? Description,
    double? Latitude,
    double? Longitude,
    bool IsActive) : IRequest;

public class UpdateServiceObjectCommandValidator : AbstractValidator<UpdateServiceObjectCommand>
{
    public UpdateServiceObjectCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование объекта обязательно.")
            .MaximumLength(200).WithMessage("Наименование не может превышать 200 символов.");

        RuleFor(v => v.Address)
            .NotEmpty().WithMessage("Адрес объекта обязателен.")
            .MaximumLength(500).WithMessage("Адрес не может превышать 500 символов.");

        RuleFor(v => v.Latitude)
            .InclusiveBetween(-90, 90).WithMessage("Широта должна быть в диапазоне от -90 до 90.")
            .When(v => v.Latitude.HasValue);

        RuleFor(v => v.Longitude)
            .InclusiveBetween(-180, 180).WithMessage("Долгота должна быть в диапазоне от -180 до 180.")
            .When(v => v.Longitude.HasValue);
    }
}

public class UpdateServiceObjectCommandHandler : IRequestHandler<UpdateServiceObjectCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly GeometryFactory _geometryFactory = new(new PrecisionModel(), 4326);

    public UpdateServiceObjectCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateServiceObjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ServiceObjects.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(ServiceObject), request.Id);

        entity.Name        = request.Name;
        entity.Address     = request.Address;
        entity.Description = request.Description;
        entity.IsActive    = request.IsActive;

        entity.Location = (request.Latitude.HasValue && request.Longitude.HasValue)
            ? _geometryFactory.CreatePoint(new Coordinate(request.Longitude.Value, request.Latitude.Value))
            : null;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteServiceObjectCommand(Guid Id) : IRequest;

public class DeleteServiceObjectCommandHandler : IRequestHandler<DeleteServiceObjectCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteServiceObjectCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteServiceObjectCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ServiceObjects.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(ServiceObject), request.Id);

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
