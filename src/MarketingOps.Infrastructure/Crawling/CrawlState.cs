using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>Bir taramanın bellekteki durumu: kuyruk, ziyaret edilenler, sayaçlar ve henüz yazılmamış atlanan URL'ler.</summary>
internal sealed class CrawlState
{
    private readonly HashSet<string> _seen = [];
    private readonly List<SkippedUrl> _pendingSkipped = [];
    private readonly HashSet<string> _sitemapUrls = [];
    private readonly List<Uri> _assets = [];
    private readonly IReadOnlyList<System.Text.RegularExpressions.Regex> _excluded;

    public Site Site { get; }
    public Uri Origin { get; }
    public RobotsRules Robots { get; set; } = RobotsRules.AllowAll;
    public Queue<Uri> Frontier { get; } = new();
    public int Discovered { get; private set; }
    public int Crawled { get; set; }
    public int Failed { get; set; }
    public bool IsFull => Discovered >= Site.CrawlSettings.MaxPages;

    /// <summary>Başlıkları kontrol edilecek iç CSS/JS dosyaları (ilk görülenler, en fazla <see cref="MaxAssets"/>).</summary>
    public IReadOnlyList<Uri> Assets => _assets;
    public const int MaxAssets = 10;

    public CrawlState(Site site)
    {
        Site = site;
        Origin = new Uri($"https://{site.Domain}/");
        _excluded = site.CrawlSettings.ExcludedPathPatterns.Select(GlobPattern.ToRegex).ToList();
    }

    /// <summary>Site domain'i ve "www." öneki iç kabul edilir.</summary>
    public bool IsInternal(Uri url)
    {
        var host = url.Host.ToLowerInvariant();
        return host == Site.Domain || host == "www." + Site.Domain;
    }

    public void Enqueue(Uri url)
    {
        if (!IsInternal(url)) return;
        var key = UrlNormalizer.Normalize(url);
        if (!_seen.Add(key)) return;

        if (_excluded.Any(r => r.IsMatch(url.AbsolutePath))) Skip(key, SkipReason.ExcludedPath);
        else if (!Robots.IsAllowed(url)) Skip(key, SkipReason.RobotsTxt);
        else if (IsFull) Skip(key, SkipReason.MaxPagesReached);
        else
        {
            Discovered++;
            Frontier.Enqueue(url);
        }
    }

    public void MarkInSitemap(Uri url) => _sitemapUrls.Add(UrlNormalizer.Normalize(url));

    public bool IsInSitemap(string normalizedUrl) => _sitemapUrls.Contains(normalizedUrl);

    public void AddAsset(string? url)
    {
        if (_assets.Count >= MaxAssets || !Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsInternal(uri)) return;
        if (_assets.All(a => a.AbsoluteUri != uri.AbsoluteUri)) _assets.Add(uri);
    }

    public IReadOnlyList<SkippedUrl> TakePendingSkipped()
    {
        var taken = _pendingSkipped.ToList();
        _pendingSkipped.Clear();
        return taken;
    }

    private void Skip(string url, SkipReason reason) => _pendingSkipped.Add(new SkippedUrl(url, reason));
}
