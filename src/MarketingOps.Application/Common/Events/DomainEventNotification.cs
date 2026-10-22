using MarketingOps.Domain.Common;
using MediatR;

namespace MarketingOps.Application.Common.Events;

/// <summary>
/// Domain olayının MediatR bildirimi. Domain katmanı MediatR'a bağımlı olmasın diye olaylar bu sarmalayıcıyla yayınlanır;
/// dinleyiciler <c>INotificationHandler&lt;DomainEventNotification&lt;FindingOpened&gt;&gt;</c> uygular.
/// </summary>
public sealed record DomainEventNotification<TEvent>(TEvent Event) : INotification where TEvent : IDomainEvent;
