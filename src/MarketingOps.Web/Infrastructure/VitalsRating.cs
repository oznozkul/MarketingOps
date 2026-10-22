namespace MarketingOps.Web.Infrastructure;

/// <summary>Google'ın Core Web Vitals eşikleri (75. yüzdelik): İyi / İyileştirilmeli / Zayıf.</summary>
public static class VitalsRating
{
    public static readonly VitalsMetric Lcp = new VitalsMetric("LCP", 2500, 4000, "ms");
    public static readonly VitalsMetric Inp = new VitalsMetric("INP", 200, 500, "ms");
    public static readonly VitalsMetric Cls = new VitalsMetric("CLS", 0.1, 0.25, "");
    public static readonly VitalsMetric Ttfb = new VitalsMetric("TTFB", 800, 1800, "ms");

    public static (string Label, TagHelpers.Tone Tone) Rate(VitalsMetric metric, double value) =>
        value <= metric.Good ? ("İyi", TagHelpers.Tone.Ok)
        : value <= metric.Poor ? ("İyileştirilmeli", TagHelpers.Tone.Warn)
        : ("Zayıf", TagHelpers.Tone.Critical);

    public static string Format(VitalsMetric metric, double value) =>
        metric.Unit == "ms" ? value >= 1000 ? $"{(value / 1000).ToString("0.0", Infrastructure.Format.Tr)} sn" : $"{value.ToString("N0", Infrastructure.Format.Tr)} ms"
        : value.ToString("0.00", Infrastructure.Format.Tr);

    public static string Tooltip(VitalsMetric metric) =>
        $"{metric.Name}: İyi ≤ {Format(metric, metric.Good)} · Zayıf > {Format(metric, metric.Poor)} (75. yüzdelik)";
}
