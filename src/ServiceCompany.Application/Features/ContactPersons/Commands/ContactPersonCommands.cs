using FluentValidation;
using MediatR;
using ServiceCompany.Application.Common.Interfaces;
using ServiceCompany.Domain.Common;
using ServiceCompany.Domain.Entities;

namespace ServiceCompany.Application.Features.ContactPersons.Commands;

public record CreateContactPersonCommand(
    Guid ClientId,
    string FirstName,
    string LastName,
    string? Position,
    string? Email,
    string? PhoneNumber) : IRequest<Guid>;

public class CreateContactPersonCommandValidator : AbstractValidator<CreateContactPersonCommand>
{
    public CreateContactPersonCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("Имя контактного лица обязательно.")
            .MaximumLength(100).WithMessage("Имя не может превышать 100 символов.");

        RuleFor(v => v.LastName)
            .NotEmpty().WithMessage("Фамилия контактного лица обязательна.")
            .MaximumLength(100).WithMessage("Фамилия не может превышать 100 символов.");

        RuleFor(v => v.ClientId)
            .NotEmpty().WithMessage("Необходимо указать клиента.");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }
}

public class CreateContactPersonCommandHandler : IRequestHandler<CreateContactPersonCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateContactPersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateContactPersonCommand request, CancellationToken cancellationToken)
    {
        var entity = new ContactPerson
        {
            ClientId    = request.ClientId,
            FirstName   = request.FirstName,
            LastName    = request.LastName,
            Position    = request.Position,
            Email       = request.Email,
            PhoneNumber = request.PhoneNumber
        };

        _context.ContactPersons.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}

public record UpdateContactPersonCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string? Position,
    string? Email,
    string? PhoneNumber) : IRequest;

public class UpdateContactPersonCommandValidator : AbstractValidator<UpdateContactPersonCommand>
{
    public UpdateContactPersonCommandValidator()
    {
        RuleFor(v => v.FirstName)
            .NotEmpty().WithMessage("Имя контактного лица обязательно.")
            .MaximumLength(100).WithMessage("Имя не может превышать 100 символов.");

        RuleFor(v => v.LastName)
            .NotEmpty().WithMessage("Фамилия контактного лица обязательна.")
            .MaximumLength(100).WithMessage("Фамилия не может превышать 100 символов.");

        RuleFor(v => v.Email)
            .EmailAddress().WithMessage("Некорректный формат e-mail.")
            .When(v => !string.IsNullOrEmpty(v.Email));
    }
}

public class UpdateContactPersonCommandHandler : IRequestHandler<UpdateContactPersonCommand>
{
    private readonly IApplicationDbContext _context;

    public UpdateContactPersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(UpdateContactPersonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ContactPersons.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(ContactPerson), request.Id);

        entity.FirstName   = request.FirstName;
        entity.LastName    = request.LastName;
        entity.Position    = request.Position;
        entity.Email       = request.Email;
        entity.PhoneNumber = request.PhoneNumber;

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public record DeleteContactPersonCommand(Guid Id) : IRequest;

public class DeleteContactPersonCommandHandler : IRequestHandler<DeleteContactPersonCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteContactPersonCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteContactPersonCommand request, CancellationToken cancellationToken)
    {
        var entity = await _context.ContactPersons.FindAsync(new object[] { request.Id }, cancellationToken);
        if (entity == null) throw new NotFoundException(nameof(ContactPerson), request.Id);

        _context.ContactPersons.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
