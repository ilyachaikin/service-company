using MediatR;

namespace ServiceCompany.Domain.Common;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
