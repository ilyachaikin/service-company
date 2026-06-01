using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.MaintenancePlans.Commands;

public record CreateMaintenancePlanCommand(
    Guid ServiceObjectId,
    Guid? EquipmentId,
    string Title,
    string? Description,
    string CronExpression,
    DateTime StartDate,
    DateTime? EndDate,
    string? ChecklistTemplateJson,
    string? DefaultEngineerId,
    TicketPriority DefaultPriority) : IRequest<Guid>;

public class CreateMaintenancePlanCommandValidator : AbstractValidator<CreateMaintenancePlanCommand>
{
    public CreateMaintenancePlanCommandValidator()
    {
        RuleFor(v => v.Title).NotEmpty().MaximumLength(200)
            .WithMessage("Название плана обязательно (максимум 200 символов)");
        RuleFor(v => v.ServiceObjectId).NotEmpty()
            .WithMessage("Необходимо указать объект обслуживания");
        RuleFor(v => v.CronExpression).NotEmpty()
            .WithMessage("Необходимо указать расписание в формате Cron");
        RuleFor(v => v.StartDate).NotEmpty()
            .WithMessage("Дата начала обязательна");
        RuleFor(v => v.EndDate).GreaterThan(v => v.StartDate)
            .When(v => v.EndDate.HasValue)
            .WithMessage("Дата окончания должна быть позже даты начала");
    }
}

public class CreateMaintenancePlanCommandHandler : IRequestHandler<CreateMaintenancePlanCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateMaintenancePlanCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateMaintenancePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = new MaintenancePlan
        {
            ServiceObjectId      = request.ServiceObjectId,
            EquipmentId          = request.EquipmentId,
            Title                = request.Title,
            Description          = request.Description,
            CronExpression       = request.CronExpression,
            StartDate            = request.StartDate,
            EndDate              = request.EndDate,
            IsActive             = true,
            ChecklistTemplateJson = request.ChecklistTemplateJson,
            DefaultEngineerId    = request.DefaultEngineerId,
            DefaultPriority      = request.DefaultPriority
        };

        _context.MaintenancePlans.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateMaintenancePlanCommand(
    Guid Id,
    string Title,
    string? Description,
    string CronExpression,
    DateTime StartDate,
    DateTime? EndDate,
    string? ChecklistTemplateJson,
    string? DefaultEngineerId,
    TicketPriority DefaultPriority) : IRequest;

public class UpdateMaintenancePlanCommandHandler : IRequestHandler<UpdateMaintenancePlanCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateMaintenancePlanCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateMaintenancePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MaintenancePlans
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(MaintenancePlan), request.Id);

        entity.Title                = request.Title;
        entity.Description          = request.Description;
        entity.CronExpression       = request.CronExpression;
        entity.StartDate            = request.StartDate;
        entity.EndDate              = request.EndDate;
        entity.ChecklistTemplateJson = request.ChecklistTemplateJson;
        entity.DefaultEngineerId    = request.DefaultEngineerId;
        entity.DefaultPriority      = request.DefaultPriority;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record ToggleMaintenancePlanCommand(Guid Id) : IRequest<bool>;

public class ToggleMaintenancePlanCommandHandler : IRequestHandler<ToggleMaintenancePlanCommand, bool>
{
    private readonly IApplicationDbContext _context;
    public ToggleMaintenancePlanCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<bool> Handle(ToggleMaintenancePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MaintenancePlans
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(MaintenancePlan), request.Id);

        entity.IsActive = !entity.IsActive;
        await _context.SaveChangesAsync(cancellationToken);
        return entity.IsActive;
    }
}

public record DeleteMaintenancePlanCommand(Guid Id) : IRequest;

public class DeleteMaintenancePlanCommandHandler : IRequestHandler<DeleteMaintenancePlanCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteMaintenancePlanCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteMaintenancePlanCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.MaintenancePlans
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(MaintenancePlan), request.Id);

        _context.MaintenancePlans.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
