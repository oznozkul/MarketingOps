using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Rendering;

/// <summary>
/// Render ölçümünde sorun bulunan bir örneğin kanıt ekran görüntüsü; sorunlu öğeler çerçevelenir. Tarama çıktısıdır,
/// sayfa kayıtlarıyla birlikte saklama süresi dolunca silinir. Görüntü JPEG, yüksekliği en fazla <see cref="MaxHeight"/> pikseldir.
/// </summary>
public class RenderScreenshot : Entity
{
    public const int UrlMaxLength = 2048;
    public const int MaxHeight = 3000;

    public Guid CrawlId { get; private set; }
    public Guid SiteId { get; private set; }
    public string Url { get; private set; } = null!;
    public RenderDevice Device { get; private set; }
    public int Height { get; private set; }
    public byte[] Bytes { get; private set; } = [];
    public DateTimeOffset CapturedAt { get; private set; }

    private RenderScreenshot() { }

    public static RenderScreenshot Create(Guid crawlId, Guid siteId, string url, RenderDevice device, int height, byte[] bytes, DateTimeOffset capturedAt)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new DomainException("Sayfa URL'i boş olamaz.");
        if (bytes.Length == 0) throw new DomainException("Ekran görüntüsü boş olamaz.");
        return new RenderScreenshot
        {
            CrawlId = crawlId,
            SiteId = siteId,
            Url = url.Length <= UrlMaxLength ? url : url[..UrlMaxLength],
            Device = device,
            Height = height,
            Bytes = bytes,
            CapturedAt = capturedAt
        };
    }
}
