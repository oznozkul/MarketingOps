using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>Yönetici özetinin gönderim zamanı. Haftalık özet pazartesi, aylık özet ayın ilk günü gider.</summary>
public class ReportSchedule : Entity
{
    private List<string> _recipients = [];

    public Guid SiteId { get; private set; }
    public ReportPeriod Period { get; private set; }
    public TimeOnly SendAt { get; private set; }
    public bool Enabled { get; private set; }
    public DateOnly? LastSentOn { get; private set; }
    public IReadOnlyList<string> Recipients => _recipients;

    private ReportSchedule() { }

    public static ReportSchedule Create(Guid siteId, ReportPeriod period, bool enabled, TimeOnly sendAt, IEnumerable<string>? recipients)
    {
        var schedule = new ReportSchedule { SiteId = siteId, Period = period };
        schedule.Update(enabled, sendAt, recipients);
        return schedule;
    }

    public void Update(bool enabled, TimeOnly sendAt, IEnumerable<string>? recipients)
    {
        var emails = NotificationAddress.Normalize(recipients);
        if (enabled && emails.Count == 0) throw new DomainException("Özet açıkken en az bir alıcı gerekli.");
        Enabled = enabled;
        SendAt = sendAt;
        _recipients.Clear();
        _recipients.AddRange(emails);
    }

    public bool IsDue(DateTimeOffset utcNow)
    {
        if (!Enabled) return false;
        var local = IstanbulClock.Local(utcNow);
        var today = DateOnly.FromDateTime(local.DateTime);
        var periodStart = Period switch
        {
            ReportPeriod.Weekly => today.AddDays(-((int)today.DayOfWeek + 6) % 7),
            ReportPeriod.Monthly => new DateOnly(today.Year, today.Month, 1),
            _ => today
        };
        if (LastSentOn >= periodStart) return false;
        return today > periodStart || TimeOnly.FromDateTime(local.DateTime) >= SendAt;
    }

    public void MarkSent(DateOnly on) => LastSentOn = on;
}
