using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Rendering;

/// <summary>
/// Bir taramada örnek seçilen sayfanın bir cihazdaki render ölçümü. Tarama çıktısıdır (PageSnapshot gibi); kendi başına
/// değişmez, sayfa kayıtlarıyla birlikte saklama süresi dolunca silinir.
/// </summary>
public class RenderSample : Entity
{
    public const int UrlMaxLength = 2048;
    public const int ErrorMaxLength = 500;

    public Guid CrawlId { get; private set; }
    public Guid SiteId { get; private set; }
    public string Url { get; private set; } = null!;
    public Guid? PageTypeId { get; private set; }
    public RenderDevice Device { get; private set; }
    public RenderStatus Status { get; private set; }
    public string? Error { get; private set; }
    public DateTimeOffset MeasuredAt { get; private set; }
    public RenderMeasurements Measurements { get; private set; } = RenderMeasurements.Empty;

    /// <summary>İlk ekran ve kontrast sonuçları güvenilir: ekranı bir katman kaplamıyordu.</summary>
    public bool HasClearView => Status == RenderStatus.Measured;

    /// <summary>Ölçüm var (engellenmiş olsa da taşma, yazı boyutu ve dokunma alanı geçerlidir).</summary>
    public bool HasMeasurements => Status is RenderStatus.Measured or RenderStatus.Obstructed;

    private RenderSample() { }

    public static RenderSample Measured(Guid crawlId, Guid siteId, string url, Guid? pageTypeId, RenderDevice device,
        RenderMeasurements measurements, bool obstructed, DateTimeOffset measuredAt) =>
        New(crawlId, siteId, url, pageTypeId, device, obstructed ? RenderStatus.Obstructed : RenderStatus.Measured, null, measurements, measuredAt);

    public static RenderSample Failed(Guid crawlId, Guid siteId, string url, Guid? pageTypeId, RenderDevice device, string error, DateTimeOffset measuredAt)
    {
        if (string.IsNullOrWhiteSpace(error)) throw new DomainException("Başarısız ölçüm için hata nedeni gerekli.");
        return New(crawlId, siteId, url, pageTypeId, device, RenderStatus.Failed,
            error.Length <= ErrorMaxLength ? error : error[..ErrorMaxLength], RenderMeasurements.Empty, measuredAt);
    }

    private static RenderSample New(Guid crawlId, Guid siteId, string url, Guid? pageTypeId, RenderDevice device, RenderStatus status,
        string? error, RenderMeasurements measurements, DateTimeOffset measuredAt)
    {
        if (string.IsNullOrWhiteSpace(url)) throw new DomainException("Sayfa URL'i boş olamaz.");
        return new RenderSample
        {
            CrawlId = crawlId,
            SiteId = siteId,
            Url = url.Length <= UrlMaxLength ? url : url[..UrlMaxLength],
            PageTypeId = pageTypeId,
            Device = device,
            Status = status,
            Error = error,
            Measurements = measurements,
            MeasuredAt = measuredAt
        };
    }
}
