using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications.Queries.GetNotificationSettings;

public sealed record NotificationRuleResponse(
    NotificationEventType EventType,
    bool Enabled,
    IReadOnlyList<NotificationChannelKind> Channels,
    string Recipients,
    TimeOnly? QuietStart,
    TimeOnly? QuietEnd,
    int RepeatIntervalHours,
    int? ScoreDropPoints);
