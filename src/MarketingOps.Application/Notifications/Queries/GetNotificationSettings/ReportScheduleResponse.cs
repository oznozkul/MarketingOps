using MarketingOps.Domain.Notifications;

namespace MarketingOps.Application.Notifications.Queries.GetNotificationSettings;

public sealed record ReportScheduleResponse(ReportPeriod Period, bool Enabled, TimeOnly SendAt, string Recipients);
