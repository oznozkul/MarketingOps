using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Crawls;

/// <summary>
/// Bir sayfanın bir taramadaki gözlemi. Tarama sırasında toplu yazılır ve sonradan değişmez;
/// bu yüzden davranışı yok, yalnızca geçerli bir kayıt oluşturulmasını sağlar.
/// </summary>
public class PageSnapshot : Entity
{
    public const int UrlMaxLength = 2048;

    public Guid CrawlId { get; private set; }
    public Guid SiteId { get; private set; }
    public string Url { get; private set; } = null!;
    public string NormalizedUrl { get; private set; } = null!;
    public Guid? PageTypeId { get; private set; }

    /// <summary>Bağlantı hatası, zaman aşımı gibi durumlarda yanıt alınamadığı için null.</summary>
    public int? StatusCode { get; private set; }
    public IReadOnlyList<RedirectHop> RedirectChain { get; private set; } = [];
    public int? ResponseTimeMs { get; private set; }
    public string? ContentType { get; private set; }
    public string? ContentEncoding { get; private set; }
    public string? CacheControl { get; private set; }
    public int? HtmlSizeBytes { get; private set; }
    public string? FetchError { get; private set; }
    public DateTimeOffset FetchedAt { get; private set; }

    /// <summary>URL sitemap'te listelenmiş mi ("noindex ama sitemap'te" kuralı için).</summary>
    public bool InSitemap { get; private set; }
    public PageSnapshotData Data { get; private set; } = PageSnapshotData.Empty;

    public bool IsFailed => StatusCode is null || StatusCode >= 400;

    private PageSnapshot() { }

    public static PageSnapshot Create(
        Guid crawlId, Guid siteId, string url, string normalizedUrl, Guid? pageTypeId,
        int? statusCode, IReadOnlyList<RedirectHop> redirectChain, int? responseTimeMs,
        string? contentType, string? contentEncoding, string? cacheControl, int? htmlSizeBytes,
        string? fetchError, DateTimeOffset fetchedAt, PageSnapshotData data, bool inSitemap = false)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new DomainException("Sayfa URL'i boş olamaz.");
        if (statusCode is null && string.IsNullOrWhiteSpace(fetchError))
            throw new DomainException("Yanıt alınamayan sayfa için hata nedeni gerekli.");

        return new PageSnapshot
        {
            CrawlId = crawlId,
            SiteId = siteId,
            Url = Truncate(url),
            NormalizedUrl = Truncate(normalizedUrl),
            PageTypeId = pageTypeId,
            StatusCode = statusCode,
            RedirectChain = redirectChain,
            ResponseTimeMs = responseTimeMs,
            ContentType = contentType,
            ContentEncoding = contentEncoding,
            CacheControl = cacheControl,
            HtmlSizeBytes = htmlSizeBytes,
            FetchError = fetchError,
            FetchedAt = fetchedAt,
            Data = data,
            InSitemap = inSitemap
        };
    }

    private static string Truncate(string value) => value.Length <= UrlMaxLength ? value : value[..UrlMaxLength];
}
