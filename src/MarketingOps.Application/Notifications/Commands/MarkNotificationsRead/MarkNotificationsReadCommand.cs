using MediatR;

namespace MarketingOps.Application.Notifications.Commands.MarkNotificationsRead;

public sealed record MarkNotificationsReadCommand(Guid SiteId) : IRequest;
