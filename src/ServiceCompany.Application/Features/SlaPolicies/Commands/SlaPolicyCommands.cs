using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.SlaPolicies.Commands;

public record CreateSlaPolicyCommand(
    string Name,
    string? Description,
    int ResponseTimeHours,
    int ResolutionTimeHours) : IRequest<Guid>;

public class CreateSlaPolicyCommandValidator : AbstractValidator<CreateSlaPolicyCommand>
{
    public CreateSlaPolicyCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование SLA-политики обязательно.")
            .MaximumLength(100).WithMessage("Наименование не может превышать 100 символов.");

        RuleFor(v => v.ResponseTimeHours)
            .GreaterThan(0).WithMessage("Время ответа должно быть больше нуля часов.");

        RuleFor(v => v.ResolutionTimeHours)
            .GreaterThan(0).WithMessage("Время решения должно быть больше нуля часов.")
            .GreaterThanOrEqualTo(v => v.ResponseTimeHours)
            .WithMessage("Время решения должно быть не меньше времени ответа.");
    }
}

public class CreateSlaPolicyCommandHandler : IRequestHandler<CreateSlaPolicyCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateSlaPolicyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var entity = new SlaPolicy
        {
            Name                = request.Name,
            Description         = request.Description,
            ResponseTimeHours   = request.ResponseTimeHours,
            ResolutionTimeHours = request.ResolutionTimeHours
        };

        _context.SlaPolicies.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateSlaPolicyCommand(
    Guid Id,
    string Name,
    string? Description,
    int ResponseTimeHours,
    int ResolutionTimeHours) : IRequest;

public class UpdateSlaPolicyCommandValidator : AbstractValidator<UpdateSlaPolicyCommand>
{
    public UpdateSlaPolicyCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование SLA-политики обязательно.")
            .MaximumLength(100).WithMessage("Наименование не может превышать 100 символов.");

        RuleFor(v => v.ResponseTimeHours)
            .GreaterThan(0).WithMessage("Время ответа должно быть больше нуля часов.");

        RuleFor(v => v.ResolutionTimeHours)
            .GreaterThan(0).WithMessage("Время решения должно быть больше нуля часов.")
            .GreaterThanOrEqualTo(v => v.ResponseTimeHours)
            .WithMessage("Время решения должно быть не меньше времени ответа.");
    }
}

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateSlaPolicyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SlaPolicies.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(SlaPolicy), request.Id);

        entity.Name                = request.Name;
        entity.Description         = request.Description;
        entity.ResponseTimeHours   = request.ResponseTimeHours;
        entity.ResolutionTimeHours = request.ResolutionTimeHours;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteSlaPolicyCommand(Guid Id) : IRequest;

public class DeleteSlaPolicyCommandHandler : IRequestHandler<DeleteSlaPolicyCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteSlaPolicyCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.SlaPolicies.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(SlaPolicy), request.Id);

        _context.SlaPolicies.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
