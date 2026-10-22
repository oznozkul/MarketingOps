using MarketingOps.Domain.Sites;

namespace MarketingOps.Domain.Tracking;

/// <summary>Yakalanan istekleri satıcı hücrelerine ve durumlarına çevirir. Kimlik tanımlı değilse yanlış ID üretilmez.</summary>
public static class TagAuditBuilder
{
    public static IReadOnlyList<TagAuditResult> Build(Guid auditRunId, IReadOnlyList<AuditedPage> pages, ExpectedTrackingIds expected)
    {
        var results = new List<TagAuditResult>();
        for (var i = 0; i < pages.Count; i++)
        {
            var page = pages[i];
            foreach (var vendor in Enum.GetValues<TagVendor>())
            {
                var requests = page.Requests.Where(r => r.Vendor == vendor).ToList();
                var found = requests.Select(r => r.Id).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).Select(id => id!).ToList();
                var expectedId = Expected(expected, vendor);
                var prefire = requests.Any(r => r.BeforeConsent && vendor is TagVendor.Ga4 or TagVendor.GoogleAds or TagVendor.MetaPixel);
                var status = Status(expectedId, found, prefire);
                results.Add(TagAuditResult.Create(auditRunId, i, page.Label, page.PageTypeId, page.Url, vendor, status, found, requests));
            }
        }
        return results;
    }

    public static string? Expected(ExpectedTrackingIds expected, TagVendor vendor) => vendor switch
    {
        TagVendor.Gtm => expected.GtmContainerId,
        TagVendor.Ga4 => expected.Ga4MeasurementId,
        TagVendor.GoogleAds => expected.GoogleAdsConversionId,
        _ => expected.MetaPixelId
    };

    /// <summary>Onay öncesi reklam/analitik isteği, yanlış kimlik, çift kimlik ve eksikten önce gelir.</summary>
    public static TagAuditStatus Status(string? expectedId, IReadOnlyList<string> found, bool prefire)
    {
        if (prefire) return TagAuditStatus.FiresBeforeConsent;
        if (expectedId is not null && found.Count > 0 && !found.Contains(expectedId, StringComparer.OrdinalIgnoreCase))
            return TagAuditStatus.WrongId;
        if (found.Count > 1) return TagAuditStatus.Duplicate;
        if (expectedId is not null && found.Count == 0) return TagAuditStatus.Missing;
        return TagAuditStatus.Ok;
    }
}
