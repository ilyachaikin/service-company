using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Contracts.Commands;

public record CreateContractCommand(
    string Number,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalAmount,
    Guid ClientId,
    Guid SlaPolicyId) : IRequest<Guid>;

public class CreateContractCommandValidator : AbstractValidator<CreateContractCommand>
{
    public CreateContractCommandValidator()
    {
        RuleFor(v => v.Number)
            .NotEmpty().WithMessage("Номер договора обязателен.")
            .MaximumLength(50).WithMessage("Номер договора не может превышать 50 символов.");

        RuleFor(v => v.StartDate)
            .NotEmpty().WithMessage("Дата начала договора обязательна.");

        RuleFor(v => v.EndDate)
            .GreaterThan(v => v.StartDate).WithMessage("Дата окончания должна быть позже даты начала.");

        RuleFor(v => v.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Сумма договора не может быть отрицательной.");

        RuleFor(v => v.ClientId)
            .NotEmpty().WithMessage("Необходимо указать клиента.");

        RuleFor(v => v.SlaPolicyId)
            .NotEmpty().WithMessage("Необходимо указать SLA-политику.");
    }
}

public class CreateContractCommandHandler : IRequestHandler<CreateContractCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateContractCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateContractCommand request, CancellationToken cancellationToken)
    {
        var entity = new Contract
        {
            Number      = request.Number,
            StartDate   = request.StartDate,
            EndDate     = request.EndDate,
            TotalAmount = request.TotalAmount,
            ClientId    = request.ClientId,
            SlaPolicyId = request.SlaPolicyId,
            Status      = ContractStatus.Active
        };

        _context.Contracts.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateContractCommand(
    Guid Id,
    string Number,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalAmount,
    ContractStatus Status,
    Guid SlaPolicyId) : IRequest;

public class UpdateContractCommandValidator : AbstractValidator<UpdateContractCommand>
{
    public UpdateContractCommandValidator()
    {
        RuleFor(v => v.Number)
            .NotEmpty().WithMessage("Номер договора обязателен.")
            .MaximumLength(50).WithMessage("Номер договора не может превышать 50 символов.");

        RuleFor(v => v.EndDate)
            .GreaterThan(v => v.StartDate).WithMessage("Дата окончания должна быть позже даты начала.");

        RuleFor(v => v.TotalAmount)
            .GreaterThanOrEqualTo(0).WithMessage("Сумма договора не может быть отрицательной.");

        RuleFor(v => v.SlaPolicyId)
            .NotEmpty().WithMessage("Необходимо указать SLA-политику.");
    }
}

public class UpdateContractCommandHandler : IRequestHandler<UpdateContractCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateContractCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateContractCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Contracts.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Contract), request.Id);

        entity.Number      = request.Number;
        entity.StartDate   = request.StartDate;
        entity.EndDate     = request.EndDate;
        entity.TotalAmount = request.TotalAmount;
        entity.Status      = request.Status;
        entity.SlaPolicyId = request.SlaPolicyId;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteContractCommand(Guid Id) : IRequest;

public class DeleteContractCommandHandler : IRequestHandler<DeleteContractCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteContractCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteContractCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Contracts.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Contract), request.Id);

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
