using MediatR;

namespace MarketingOps.Application.Notifications.Commands.OpenNotification;

public sealed record OpenNotificationCommand(Guid SiteId, Guid NotificationId) : IRequest<string?>;
