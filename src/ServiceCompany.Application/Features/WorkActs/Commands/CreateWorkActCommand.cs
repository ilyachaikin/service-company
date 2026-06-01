using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.WorkActs.Commands;

public record CreateWorkActCommand(
    string Number,
    DateTime WorkDate,
    string Description,
    decimal LaborCost,
    decimal MaterialsCost,
    Guid TicketId,
    string? PerformedByUserId) : IRequest<Guid>;

public class CreateWorkActCommandValidator : AbstractValidator<CreateWorkActCommand>
{
    public CreateWorkActCommandValidator()
    {
        RuleFor(v => v.Number)
            .NotEmpty().WithMessage("Номер акта обязателен.")
            .MaximumLength(50).WithMessage("Номер акта не может превышать 50 символов.");

        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Описание выполненных работ обязательно.");

        RuleFor(v => v.TicketId)
            .NotEmpty().WithMessage("Необходимо указать заявку.");

        RuleFor(v => v.LaborCost)
            .GreaterThanOrEqualTo(0).WithMessage("Стоимость работ не может быть отрицательной.");

        RuleFor(v => v.MaterialsCost)
            .GreaterThanOrEqualTo(0).WithMessage("Стоимость материалов не может быть отрицательной.");
    }
}

public class CreateWorkActCommandHandler : IRequestHandler<CreateWorkActCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public CreateWorkActCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateWorkActCommand request, CancellationToken cancellationToken)
    {

        var ticket = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        if (ticket == null) throw new NotFoundException(nameof(Ticket), request.TicketId);

        var entity = new WorkAct
        {
            Number            = request.Number,
            WorkDate          = request.WorkDate,
            Description       = request.Description,
            LaborCost         = request.LaborCost,
            MaterialsCost     = request.MaterialsCost,
            TotalCost         = request.LaborCost + request.MaterialsCost,
            TicketId          = request.TicketId,
            PerformedByUserId = request.PerformedByUserId
        };

        _context.WorkActs.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
