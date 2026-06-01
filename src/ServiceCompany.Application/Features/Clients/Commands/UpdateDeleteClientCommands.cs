using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.Clients.Commands;

public record UpdateClientCommand(
    Guid Id,
    string Name,
    string Inn,
    string? Address,
    string? Email,
    string? PhoneNumber,
    bool IsActive) : IRequest;

public class UpdateClientCommandValidator : AbstractValidator<UpdateClientCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateClientCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(v => v.Name)
            .NotEmpty().WithMessage("Наименование клиента обязательно.")
            .MaximumLength(200).WithMessage("Наименование не может превышать 200 символов.");

        RuleFor(v => v.Inn)
            .NotEmpty().WithMessage("ИНН обязателен.")
            .Matches(@"^\d{10}(\d{2})?$").WithMessage("ИНН должен содержать 10 или 12 цифр.")
            .MustAsync(BeUniqueInn).WithMessage("Клиент с таким ИНН уже существует.");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }

    private async Task<bool> BeUniqueInn(UpdateClientCommand model, string inn, CancellationToken cancellationToken)
    {
        return !await _context.Clients
            .AnyAsync(l => l.Inn == inn && l.Id != model.Id, cancellationToken);
    }
}

public class UpdateClientCommandHandler : IRequestHandler<UpdateClientCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateClientCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateClientCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Clients
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(Client), request.Id);

        entity.Name = request.Name;
        entity.Inn = request.Inn;
        entity.Address = request.Address;
        entity.Email = request.Email;
        entity.PhoneNumber = request.PhoneNumber;
        entity.IsActive = request.IsActive;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteClientCommand(Guid Id) : IRequest;

public class DeleteClientCommandHandler : IRequestHandler<DeleteClientCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteClientCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteClientCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.Clients
            .FindAsync(new object[] { request.Id }, cancellationToken);

        if (entity == null) throw new NotFoundException(nameof(Client), request.Id);

        _context.Clients.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
