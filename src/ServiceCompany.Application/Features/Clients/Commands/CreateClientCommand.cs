using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.Clients.Commands;

public record CreateClientCommand(
    string Name,
    string Inn,
    string? Address,
    string? Email,
    string? PhoneNumber) : IRequest<Guid>;

public class CreateClientCommandValidator : AbstractValidator<CreateClientCommand>
{
    private readonly IApplicationDbContext _context;

    public CreateClientCommandValidator(IApplicationDbContext context)
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

    private async Task<bool> BeUniqueInn(string inn, CancellationToken cancellationToken)
    {
        return !await _context.Clients
            .AnyAsync(l => l.Inn == inn, cancellationToken);
    }
}

public class CreateClientCommandHandler : IRequestHandler<CreateClientCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateClientCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var entity = new Client
        {
            Name = request.Name,
            Inn = request.Inn,
            Address = request.Address,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        _context.Clients.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
