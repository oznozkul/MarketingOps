using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Commands.DispatchNotification;

public sealed record DispatchNotificationCommand(
    Guid SiteId,
    NotificationEventType EventType,
    Guid? FindingId,
    int? Magnitude,
    string? Detail) : IRequest;
