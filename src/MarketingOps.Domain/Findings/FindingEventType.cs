namespace MarketingOps.Domain.Findings;

public enum FindingEventType
{
    Opened,
    SeenAgain,
    Resolved,
    Reopened,
    MarkedFalsePositive,
    FalsePositiveReverted,
    Snoozed,
    SnoozeExpired
}
