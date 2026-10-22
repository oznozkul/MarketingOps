using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Metrics;

/// <summary>GA4'ten günlük e-ticaret event sayısı ve değeri. (SiteId, Date, EventName) benzersizdir.</summary>
public class Ga4EventDaily : Entity
{
    public Guid SiteId { get; private set; }
    public DateOnly Date { get; private set; }
    public string EventName { get; private set; } = null!;
    public long EventCount { get; private set; }
    public decimal EventValue { get; private set; }

    private Ga4EventDaily() { }

    public static Ga4EventDaily Create(Guid siteId, DateOnly date, string eventName, long count, decimal value)
    {
        var row = new Ga4EventDaily { SiteId = siteId, Date = date, EventName = eventName };
        row.Update(count, value);
        return row;
    }

    public void Update(long count, decimal value)
    {
        if (count < 0) throw new DomainException("Event sayısı negatif olamaz.");
        EventCount = count;
        EventValue = value;
    }
}
