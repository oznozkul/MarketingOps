namespace MarketingOps.Domain.Metrics;

/// <summary>Search Console'a gönderilmiş bir sitemap'in son durumu.</summary>
public class SitemapStatus : Common.Entity
{
    public Guid SiteId { get; private set; }
    public string Path { get; private set; } = null!;
    public DateTimeOffset? LastSubmitted { get; private set; }
    public DateTimeOffset? LastDownloaded { get; private set; }
    public bool IsPending { get; private set; }
    public int Errors { get; private set; }
    public int Warnings { get; private set; }
    public long? Submitted { get; private set; }
    public long? Indexed { get; private set; }
    public DateTimeOffset SyncedAt { get; private set; }

    private SitemapStatus() { }

    public static SitemapStatus Create(Guid siteId, string path) => new() { SiteId = siteId, Path = path };

    public void Update(DateTimeOffset? lastSubmitted, DateTimeOffset? lastDownloaded, bool isPending, int errors, int warnings, long? submitted, long? indexed, DateTimeOffset syncedAt)
    {
        LastSubmitted = lastSubmitted;
        LastDownloaded = lastDownloaded;
        IsPending = isPending;
        Errors = errors;
        Warnings = warnings;
        Submitted = submitted;
        Indexed = indexed;
        SyncedAt = syncedAt;
    }
}
