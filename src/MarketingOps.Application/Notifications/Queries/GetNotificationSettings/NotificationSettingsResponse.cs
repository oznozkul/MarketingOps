namespace MarketingOps.Application.Notifications.Queries.GetNotificationSettings;

public sealed record NotificationSettingsResponse(
    bool HasTeams,
    bool HasSlack,
    bool MailConfigured,
    IReadOnlyList<NotificationRuleResponse> Rules,
    IReadOnlyList<ReportScheduleResponse> Reports);
