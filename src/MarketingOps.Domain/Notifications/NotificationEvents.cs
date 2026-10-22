using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules.Definitions;

namespace MarketingOps.Domain.Notifications;

/// <summary>Açılan bulgunun hangi bildirime denk geldiği. Purchase farkı ayrıca kritik sayılmaz.</summary>
public static class NotificationEvents
{
    public static NotificationEventType? ForOpened(string ruleCode, Severity severity)
    {
        if (ruleCode == new PurchaseGapRule().Code) return NotificationEventType.PurchaseGap;
        if (severity == Severity.Critical) return NotificationEventType.NewCriticalFinding;
        return null;
    }
}
