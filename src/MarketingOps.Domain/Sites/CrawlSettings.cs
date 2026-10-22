using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Sites;

public sealed record CrawlSettings
{
    public const int MinRequestsPerSecond = 1;
    public const int MaxRequestsPerSecondLimit = 10;
    public const int MaxPagesLimit = 500_000;

    public CrawlSchedule Schedule { get; }
    public int MaxRequestsPerSecond { get; }
    public int MaxPages { get; }
    public IReadOnlyList<string> ExcludedPathPatterns { get; }

    /// <summary>robots.txt'ye uyulması ayarlanamaz; ekranda bilgi olarak gösterilir.</summary>
    public bool RespectRobotsTxt => true;

    /// <summary>Her çağrıda yeni örnek: EF owned type olarak iki siteye aynı nesne bağlanamaz.</summary>
    public static CrawlSettings Default => new(CrawlSchedule.Daily, 2, 10_000, []);

    public CrawlSettings(CrawlSchedule schedule, int maxRequestsPerSecond, int maxPages, IEnumerable<string> excludedPathPatterns)
    {
        if (maxRequestsPerSecond is < MinRequestsPerSecond or > MaxRequestsPerSecondLimit)
            throw new DomainException($"Hız limiti {MinRequestsPerSecond} ile {MaxRequestsPerSecondLimit} istek/saniye arasında olmalı.");
        if (maxPages is < 1 or > MaxPagesLimit)
            throw new DomainException($"Maksimum sayfa sayısı 1 ile {MaxPagesLimit:N0} arasında olmalı.");

        Schedule = schedule;
        MaxRequestsPerSecond = maxRequestsPerSecond;
        MaxPages = maxPages;
        ExcludedPathPatterns = excludedPathPatterns
            .Select(p => p.Trim())
            .Where(p => p.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    // EF Core materialization için.
    private CrawlSettings()
    {
        ExcludedPathPatterns = [];
    }
}
