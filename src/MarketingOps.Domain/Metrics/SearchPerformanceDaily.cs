using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Metrics;

/// <summary>Search Console'dan sayfa bazında günlük performans. (SiteId, Date, Url) benzersizdir; tekrar senkronda güncellenir.</summary>
public class SearchPerformanceDaily : Entity
{
    public Guid SiteId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Url { get; private set; } = null!;
    public int Clicks { get; private set; }
    public int Impressions { get; private set; }
    public double Ctr { get; private set; }
    public double Position { get; private set; }

    private SearchPerformanceDaily() { }

    public static SearchPerformanceDaily Create(Guid siteId, DateOnly date, string url, int clicks, int impressions, double ctr, double position)
    {
        var row = new SearchPerformanceDaily { SiteId = siteId, Date = date, Url = url };
        row.Update(clicks, impressions, ctr, position);
        return row;
    }

    public void Update(int clicks, int impressions, double ctr, double position)
    {
        if (clicks < 0 || impressions < 0) throw new DomainException("Tıklama ve gösterim negatif olamaz.");
        Clicks = clicks;
        Impressions = impressions;
        Ctr = ctr;
        Position = position;
    }
}
