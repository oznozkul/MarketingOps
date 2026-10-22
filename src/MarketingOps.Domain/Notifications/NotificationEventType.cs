namespace MarketingOps.Domain.Notifications;

public enum NotificationEventType
{
    NewCriticalFinding,
    FindingReopened,
    PurchaseGap,
    ScoreDropped,
    CrawlFailed,
    ConnectorAuthorizationExpired
}
