namespace MarketingOps.Domain.Metrics;

/// <summary>Search Console URL Inspection sonucu; her URL için son durum tutulur.</summary>
public class UrlIndexStatus : Common.Entity
{
    public const string PassVerdict = "PASS";

    public Guid SiteId { get; private set; }
    public string Url { get; private set; } = null!;

    /// <summary>PASS, PARTIAL, FAIL, NEUTRAL (Google'ın değerleri).</summary>
    public string Verdict { get; private set; } = null!;
    public string? CoverageState { get; private set; }
    public string? IndexingState { get; private set; }
    public DateTimeOffset? LastCrawlTime { get; private set; }
    public DateTimeOffset InspectedAt { get; private set; }

    public bool IsIndexed => Verdict == PassVerdict;

    private UrlIndexStatus() { }

    public static UrlIndexStatus Create(Guid siteId, string url) => new() { SiteId = siteId, Url = url };

    public void Update(string verdict, string? coverageState, string? indexingState, DateTimeOffset? lastCrawlTime, DateTimeOffset inspectedAt)
    {
        Verdict = verdict;
        CoverageState = coverageState;
        IndexingState = indexingState;
        LastCrawlTime = lastCrawlTime;
        InspectedAt = inspectedAt;
    }
}
