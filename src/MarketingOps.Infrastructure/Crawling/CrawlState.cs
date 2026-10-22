using System.Text.RegularExpressions;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Bir taramanın bellekteki durumu: kuyruk, ziyaret edilenler, sayaçlar ve henüz yazılmamış atlanan URL'ler.
/// İndirme işçileri aynı anda eriştiği için tüm değişiklikler tek kilit altındadır.
/// </summary>
internal sealed class CrawlState
{
    public const int MaxAssets = 10;

    private readonly object _lock = new();
    private readonly HashSet<string> _seen = [];
    private readonly Queue<Uri> _frontier = new();
    private readonly List<SkippedUrl> _pendingSkipped = [];
    private readonly HashSet<string> _sitemapUrls = [];
    private readonly List<Uri> _assets = [];
    private readonly HashSet<string> _retried = [];
    private readonly IReadOnlyList<Regex> _excluded;
    private int _discovered;
    private int _crawled;
    private int _failed;
    private int _inFlight;

    public Site Site { get; }
    public Uri Origin { get; }
    public RobotsRules Robots { get; set; } = RobotsRules.AllowAll;

    public CrawlState(Site site)
    {
        Site = site;
        Origin = new Uri($"https://{site.Domain}/");
        _excluded = site.CrawlSettings.ExcludedPathPatterns.Select(GlobPattern.ToRegex).ToList();
    }

    public int Discovered { get { lock (_lock) return _discovered; } }
    public int Crawled { get { lock (_lock) return _crawled; } }
    public int Failed { get { lock (_lock) return _failed; } }
    public bool IsFull { get { lock (_lock) return _discovered >= Site.CrawlSettings.MaxPages; } }

    /// <summary>Kuyruk boş ve işlenen sayfa yok: tarama bitti.</summary>
    public bool IsDrained { get { lock (_lock) return _frontier.Count == 0 && _inFlight == 0; } }

    /// <summary>Başlıkları kontrol edilecek iç CSS/JS dosyaları (ilk görülenler, en fazla <see cref="MaxAssets"/>).</summary>
    public IReadOnlyList<Uri> Assets { get { lock (_lock) return _assets.ToList(); } }

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
        lock (_lock)
        {
            if (!_seen.Add(key)) return;
            if (_excluded.Any(r => r.IsMatch(url.AbsolutePath))) Skip(key, SkipReason.ExcludedPath);
            else if (!Robots.IsAllowed(url)) Skip(key, SkipReason.RobotsTxt);
            else if (_discovered >= Site.CrawlSettings.MaxPages) Skip(key, SkipReason.MaxPagesReached);
            else
            {
                _discovered++;
                _frontier.Enqueue(url);
            }
        }
    }

    /// <summary>Kuyruktan bir adres alır ve işlenmekte olarak sayar; işlem bitince <see cref="Complete"/> çağrılmalı.</summary>
    public bool TryTake(out Uri url)
    {
        lock (_lock)
        {
            if (_frontier.TryDequeue(out url!))
            {
                _inFlight++;
                return true;
            }
            return false;
        }
    }

    public void Complete(bool failed)
    {
        lock (_lock)
        {
            _inFlight--;
            if (failed) _failed++;
            else _crawled++;
        }
    }

    /// <summary>Sunucu "yavaşla" dediğinde (429/503) adres bir kez kuyruğun sonuna döner. İkinci kez reddedilirse false.</summary>
    public bool Requeue(Uri url)
    {
        lock (_lock)
        {
            _inFlight--;
            if (!_retried.Add(UrlNormalizer.Normalize(url))) return false;
            _frontier.Enqueue(url);
            return true;
        }
    }

    /// <summary>Kaldığı yerden devam: önceki işçinin yazdığı sayfalar ve sayaçlar geri yüklenir.</summary>
    public void Restore(IEnumerable<string> crawledKeys, int crawled, int failed)
    {
        lock (_lock)
        {
            foreach (var key in crawledKeys) _seen.Add(key);
            _discovered = _seen.Count;
            _crawled = crawled;
            _failed = failed;
        }
    }

    public void MarkInSitemap(Uri url)
    {
        var key = UrlNormalizer.Normalize(url);
        lock (_lock) _sitemapUrls.Add(key);
    }

    public bool IsInSitemap(string normalizedUrl)
    {
        lock (_lock) return _sitemapUrls.Contains(normalizedUrl);
    }

    public void AddAsset(string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !IsInternal(uri)) return;
        lock (_lock)
        {
            if (_assets.Count < MaxAssets && _assets.All(a => a.AbsoluteUri != uri.AbsoluteUri)) _assets.Add(uri);
        }
    }

    public IReadOnlyList<SkippedUrl> TakePendingSkipped()
    {
        lock (_lock)
        {
            var taken = _pendingSkipped.ToList();
            _pendingSkipped.Clear();
            return taken;
        }
    }

    private void Skip(string url, SkipReason reason) => _pendingSkipped.Add(new SkippedUrl(url, reason));
}
