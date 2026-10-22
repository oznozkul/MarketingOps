using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Commands.SendTestNotification;

public sealed record SendTestNotificationCommand(Guid SiteId, NotificationChannelKind Channel) : IRequest<string>;
