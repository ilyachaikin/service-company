using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Equipments.Commands;

public record CreateEquipmentCommand(
    string Name,
    string? SerialNumber,
    string? Model,
    string? Manufacturer,
    DateTime? PurchaseDate,
    DateTime? WarrantyExpiryDate,
    Guid ServiceObjectId) : IRequest<Guid>;

public class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование оборудования обязательно.")
            .MaximumLength(200).WithMessage("Наименование не может превышать 200 символов.");

        RuleFor(v => v.ServiceObjectId)
            .NotEmpty().WithMessage("Необходимо указать объект обслуживания.");

        RuleFor(v => v.WarrantyExpiryDate)
            .GreaterThan(v => v.PurchaseDate).WithMessage("Дата окончания гарантии должна быть позже даты покупки.")
            .When(v => v.PurchaseDate.HasValue && v.WarrantyExpiryDate.HasValue);
    }
}

public class CreateEquipmentCommandHandler : IRequestHandler<CreateEquipmentCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateEquipmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var entity = new Equipment
        {
            Name               = request.Name,
            SerialNumber       = request.SerialNumber,
            Model              = request.Model,
            Manufacturer       = request.Manufacturer,
            PurchaseDate       = request.PurchaseDate,
            WarrantyExpiryDate = request.WarrantyExpiryDate,
            ServiceObjectId    = request.ServiceObjectId,
            Status             = EquipmentStatus.Working,
            IsActive           = true
        };

        _context.Equipments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateEquipmentCommand(
    Guid Id,
    string Name,
    string? SerialNumber,
    string? Model,
    string? Manufacturer,
    DateTime? PurchaseDate,
    DateTime? WarrantyExpiryDate,
    EquipmentStatus Status,
    bool IsActive) : IRequest;

public class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование оборудования обязательно.")
            .MaximumLength(200).WithMessage("Наименование не может превышать 200 символов.");

        RuleFor(v => v.WarrantyExpiryDate)
            .GreaterThan(v => v.PurchaseDate).WithMessage("Дата окончания гарантии должна быть позже даты покупки.")
            .When(v => v.PurchaseDate.HasValue && v.WarrantyExpiryDate.HasValue);
    }
}

public class UpdateEquipmentCommandHandler : IRequestHandler<UpdateEquipmentCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateEquipmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateEquipmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Equipments.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Equipment), request.Id);

        entity.Name               = request.Name;
        entity.SerialNumber       = request.SerialNumber;
        entity.Model              = request.Model;
        entity.Manufacturer       = request.Manufacturer;
        entity.PurchaseDate       = request.PurchaseDate;
        entity.WarrantyExpiryDate = request.WarrantyExpiryDate;
        entity.Status             = request.Status;
        entity.IsActive           = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteEquipmentCommand(Guid Id) : IRequest;

public class DeleteEquipmentCommandHandler : IRequestHandler<DeleteEquipmentCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteEquipmentCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteEquipmentCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Equipments.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Equipment), request.Id);

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
