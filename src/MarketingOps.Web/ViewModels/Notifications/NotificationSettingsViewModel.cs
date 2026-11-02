namespace MarketingOps.Web.ViewModels.Notifications;

public sealed record NotificationSettingsViewModel(
    NotificationChannelsForm Channels,
    IReadOnlyList<NotificationRuleForm> Rules,
    IReadOnlyList<ReportScheduleForm> Reports);
