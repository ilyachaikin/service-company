using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Tickets.Commands;

public record CreateTicketCommand(
    string Title,
    string Description,
    TicketPriority Priority,
    TicketType Type,
    Guid ClientId,
    Guid? ServiceObjectId,
    Guid? EquipmentId,
    DateTime? DueDate) : IRequest<Guid>;

public class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty().WithMessage("Тема заявки обязательна.")
            .MaximumLength(200).WithMessage("Тема не может превышать 200 символов.");

        RuleFor(v => v.Description)
            .NotEmpty().WithMessage("Описание заявки обязательно.");

        RuleFor(v => v.ClientId)
            .NotEmpty().WithMessage("Необходимо указать клиента.");

        RuleFor(v => v.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Желаемая дата исполнения должна быть в будущем.")
            .When(v => v.DueDate.HasValue);
    }
}

public class CreateTicketCommandHandler : IRequestHandler<CreateTicketCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateTicketCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateTicketCommand request, CancellationToken cancellationToken)
    {
        var entity = new Ticket
        {
            Title           = request.Title,
            Description     = request.Description,
            Priority        = request.Priority,
            Type            = request.Type,
            ClientId        = request.ClientId,
            ServiceObjectId = request.ServiceObjectId,
            EquipmentId     = request.EquipmentId,
            DueDate         = request.DueDate,
            Status          = TicketStatus.New
        };

        _context.Tickets.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateTicketStatusCommand(
    Guid TicketId,
    TicketStatus NewStatus,
    string? Comment) : IRequest;

public class UpdateTicketStatusCommandValidator : AbstractValidator<UpdateTicketStatusCommand>
{
    public UpdateTicketStatusCommandValidator()
    {
        RuleFor(v => v.TicketId)
            .NotEmpty().WithMessage("Идентификатор заявки обязателен.");
    }
}

public class UpdateTicketStatusCommandHandler : IRequestHandler<UpdateTicketStatusCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UpdateTicketStatusCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task Handle(UpdateTicketStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Ticket), request.TicketId);

        if (entity.Status == request.NewStatus)
            throw new InvalidOperationException(
                $"Заявка уже имеет статус «{entity.Status}». Выберите другой статус.");

        if (entity.Status == TicketStatus.Archived)
            throw new InvalidOperationException("Архивная заявка не может быть изменена.");

        if (entity.Status == TicketStatus.Closed
            && request.NewStatus != TicketStatus.InProgress
            && request.NewStatus != TicketStatus.Archived)
        {
            throw new InvalidOperationException(
                "Закрытая заявка может быть только переоткрыта (взять в работу) или отправлена в архив.");
        }

        var oldStatus = entity.Status;
        entity.Status = request.NewStatus;

        if (request.NewStatus == TicketStatus.Done || request.NewStatus == TicketStatus.Closed)
        {
            entity.CompletedAt = DateTime.UtcNow;
        }

        if (request.NewStatus == TicketStatus.InProgress && oldStatus is TicketStatus.Closed or TicketStatus.Done)
        {
            entity.CompletedAt = null;
        }

        var history = new TicketStatusHistory
        {
            TicketId        = entity.Id,
            OldStatus       = oldStatus,
            NewStatus       = request.NewStatus,
            ChangedByUserId = _currentUserService.UserId,
            Comment         = request.Comment
        };

        _context.TicketStatusHistory.Add(history);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record AssignTicketCommand(Guid TicketId, string? AssignedUserId) : IRequest;

public class AssignTicketCommandValidator : AbstractValidator<AssignTicketCommand>
{
    public AssignTicketCommandValidator()
    {
        RuleFor(v => v.TicketId)
            .NotEmpty().WithMessage("Идентификатор заявки обязателен.");
    }
}

public class AssignTicketCommandHandler : IRequestHandler<AssignTicketCommand>
{
    private readonly IApplicationDbContext _context;

    public AssignTicketCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(AssignTicketCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Tickets.FindAsync(new object[] { request.TicketId }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Ticket), request.TicketId);

        entity.AssignedUserId = request.AssignedUserId;

        if (entity.Status == TicketStatus.New && !string.IsNullOrEmpty(request.AssignedUserId))
        {
            entity.Status = TicketStatus.Assigned;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record AddTicketCommentCommand(Guid TicketId, string Text, bool IsInternal) : IRequest<Guid>;

public class AddTicketCommentCommandValidator : AbstractValidator<AddTicketCommentCommand>
{
    public AddTicketCommentCommandValidator()
    {
        RuleFor(v => v.TicketId)
            .NotEmpty().WithMessage("Идентификатор заявки обязателен.");

        RuleFor(v => v.Text)
            .NotEmpty().WithMessage("Текст комментария не может быть пустым.")
            .MaximumLength(2000).WithMessage("Комментарий не может превышать 2000 символов.");
    }
}

public class AddTicketCommentCommandHandler : IRequestHandler<AddTicketCommentCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public AddTicketCommentCommandHandler(IApplicationDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Guid> Handle(AddTicketCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = new TicketComment
        {
            TicketId   = request.TicketId,
            Text       = request.Text,
            IsInternal = request.IsInternal,
            UserId     = _currentUserService.UserId ?? "System"
        };

        _context.TicketComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return comment.Id;
    }
}
