namespace MarketingOps.Domain.Tracking;

public enum TagAuditStatus
{
    Ok,
    Missing,
    Duplicate,
    WrongId,
    FiresBeforeConsent
}
