using System.Text.RegularExpressions;
using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Sites;

/// <summary>Tracking denetiminin "yanlış ID" tespiti için referans aldığı kimlikler. Hepsi opsiyoneldir.</summary>
public sealed partial record ExpectedTrackingIds
{
    public string? Ga4MeasurementId { get; }
    public string? GtmContainerId { get; }
    public string? MetaPixelId { get; }
    public string? GoogleAdsConversionId { get; }

    /// <summary>Her çağrıda yeni örnek: EF owned type olarak iki siteye aynı nesne bağlanamaz.</summary>
    public static ExpectedTrackingIds None => new(null, null, null, null);

    public ExpectedTrackingIds(string? ga4MeasurementId, string? gtmContainerId, string? metaPixelId, string? googleAdsConversionId)
    {
        Ga4MeasurementId = Validate(ga4MeasurementId, Ga4Regex(), "GA4 Measurement ID", "G-XXXXXXXXXX");
        GtmContainerId = Validate(gtmContainerId, GtmRegex(), "GTM Container ID", "GTM-XXXXXXX");
        MetaPixelId = Validate(metaPixelId, MetaPixelRegex(), "Meta Pixel ID", "15 haneli sayı");
        GoogleAdsConversionId = Validate(googleAdsConversionId, GoogleAdsRegex(), "Google Ads Conversion ID", "AW-XXXXXXXXX");
    }

    private ExpectedTrackingIds() { }

    private static string? Validate(string? value, Regex format, string label, string example)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim().ToUpperInvariant();
        if (!format.IsMatch(normalized))
            throw new DomainException($"{label} biçimi geçersiz. Beklenen: {example}.");
        return normalized;
    }

    [GeneratedRegex("^G-[A-Z0-9]{4,12}$")]
    private static partial Regex Ga4Regex();

    [GeneratedRegex("^GTM-[A-Z0-9]{4,10}$")]
    private static partial Regex GtmRegex();

    [GeneratedRegex("^[0-9]{10,20}$")]
    private static partial Regex MetaPixelRegex();

    [GeneratedRegex("^AW-[0-9]{6,15}$")]
    private static partial Regex GoogleAdsRegex();
}
