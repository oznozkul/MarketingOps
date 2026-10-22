namespace MarketingOps.Domain.Metrics;

/// <summary>
/// Bir örnek sayfanın günlük Core Web Vitals ölçümü (PageSpeed Insights / CrUX). Sayfa tipi ölçüm anındaki tiptir.
/// (SiteId, Date, Url, Strategy) benzersizdir. Değeri olmayan metrik null'dır (ör. düşük trafikli sayfada INP).
/// </summary>
public class WebVitalsDaily : Common.Entity
{
    public Guid SiteId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Url { get; private set; } = null!;
    public Guid? PageTypeId { get; private set; }
    public DeviceStrategy Strategy { get; private set; }
    public VitalsSource Source { get; private set; }
    public int? LcpMs { get; private set; }
    public int? InpMs { get; private set; }
    public double? Cls { get; private set; }
    public int? TtfbMs { get; private set; }

    private WebVitalsDaily() { }

    public static WebVitalsDaily Create(Guid siteId, DateOnly date, string url, Guid? pageTypeId, DeviceStrategy strategy) =>
        new() { SiteId = siteId, Date = date, Url = url, PageTypeId = pageTypeId, Strategy = strategy };

    public void Update(Guid? pageTypeId, VitalsSource source, int? lcpMs, int? inpMs, double? cls, int? ttfbMs)
    {
        PageTypeId = pageTypeId;
        Source = source;
        LcpMs = lcpMs;
        InpMs = inpMs;
        Cls = cls;
        TtfbMs = ttfbMs;
    }
}
