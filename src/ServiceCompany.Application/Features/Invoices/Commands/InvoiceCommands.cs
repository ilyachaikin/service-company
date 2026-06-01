using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;
using ServiceCompany.Domain.Enums;

namespace ServiceCompany.Application.Features.Invoices.Commands;

public record GenerateInvoiceCommand(Guid ClientId, List<Guid> WorkActIds, DateTime DueDate) : IRequest<Guid>;

public class GenerateInvoiceCommandValidator : AbstractValidator<GenerateInvoiceCommand>
{
    public GenerateInvoiceCommandValidator()
    {
        RuleFor(v => v.ClientId)
            .NotEmpty().WithMessage("Необходимо указать клиента.");

        RuleFor(v => v.WorkActIds)
            .NotEmpty().WithMessage("Необходимо выбрать хотя бы один акт выполненных работ.");

        RuleFor(v => v.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Дата оплаты должна быть в будущем.");
    }
}

public class GenerateInvoiceCommandHandler : IRequestHandler<GenerateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    public GenerateInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<Guid> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var workActs = await _context.WorkActs
            .Where(w => request.WorkActIds.Contains(w.Id) && w.InvoiceId == null)
            .ToListAsync(cancellationToken);

        if (!workActs.Any())
            throw new InvalidOperationException("Нет доступных актов для выставления счёта. Возможно, они уже включены в другой счёт.");

        var totalAmount = workActs.Sum(w => w.TotalCost);

        var lastNumber = await _context.Invoices
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => i.Number)
            .FirstOrDefaultAsync(cancellationToken);

        var nextSeq = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length >= 3 && int.TryParse(parts[^1], out var parsed))
                nextSeq = parsed + 1;
        }

        var invoiceNumber = $"СЧ-{DateTime.UtcNow:yyyy}-{nextSeq:D4}";

        var invoice = new Invoice
        {
            Number     = invoiceNumber,
            ClientId   = request.ClientId,
            Amount     = totalAmount,
            Status     = InvoiceStatus.Draft,
            IssuedDate = DateTime.UtcNow,
            DueDate    = request.DueDate,
            WorkActs   = workActs
        };

        _context.Invoices.Add(invoice);
        await _context.SaveChangesAsync(cancellationToken);

        return invoice.Id;
    }
}

public record UpdateInvoiceStatusCommand(Guid Id, InvoiceStatus Status) : IRequest;

public class UpdateInvoiceStatusCommandValidator : AbstractValidator<UpdateInvoiceStatusCommand>
{
    public UpdateInvoiceStatusCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Идентификатор счёта обязателен.");
    }
}

public class UpdateInvoiceStatusCommandHandler : IRequestHandler<UpdateInvoiceStatusCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateInvoiceStatusCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateInvoiceStatusCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Invoices.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Invoice), request.Id);

        entity.Status = request.Status;
        if (request.Status == InvoiceStatus.Paid)
        {
            entity.PaidDate = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteInvoiceCommand(Guid Id) : IRequest;

public class DeleteInvoiceCommandHandler : IRequestHandler<DeleteInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    public DeleteInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(DeleteInvoiceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Invoices.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Invoice), request.Id);

        entity.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record UpdateInvoiceCommand(Guid Id, DateTime DueDate, string? Notes) : IRequest;

public class UpdateInvoiceCommandHandler : IRequestHandler<UpdateInvoiceCommand>
{
    private readonly IApplicationDbContext _context;
    public UpdateInvoiceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task Handle(UpdateInvoiceCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Invoices.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(Invoice), request.Id);

        entity.DueDate = request.DueDate;
        await _context.SaveChangesAsync(cancellationToken);
    }
}
