using MarketingOps.Domain.Notifications;
using MediatR;

namespace MarketingOps.Application.Notifications.Commands.SaveNotificationRule;

public sealed record SaveNotificationRuleCommand(
    Guid SiteId,
    NotificationEventType EventType,
    bool Enabled,
    IReadOnlyList<NotificationChannelKind> Channels,
    string? Recipients,
    TimeOnly? QuietStart,
    TimeOnly? QuietEnd,
    int RepeatIntervalHours,
    int? ScoreDropPoints) : IRequest;
