namespace MarketingOps.Domain.Tracking;

/// <summary>Yakalanan istek URL'sini GTM, GA4, Google Ads ya da Meta Pixel olarak ayırır. Tag değilse null döner.</summary>
public static class TagRequestClassifier
{
    public static CapturedTagRequest? Classify(string url, bool beforeConsent)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return null;
        var host = uri.Host;
        var path = uri.AbsolutePath;
        var query = Query(uri);
        var gcs = Value(query, "gcs");
        var gcd = Value(query, "gcd");

        if (host.Contains("googletagmanager.com", StringComparison.OrdinalIgnoreCase) && path.Contains("gtm.js", StringComparison.OrdinalIgnoreCase))
            return new CapturedTagRequest(url, TagVendor.Gtm, Value(query, "id"), gcs, gcd, beforeConsent);

        // gtag/js hem GA4 (G-) hem Google Ads (AW-) yükler. Kimliğin öneki satıcıyı belirler.
        if (host.Contains("googletagmanager.com", StringComparison.OrdinalIgnoreCase) && path.Contains("gtag/js", StringComparison.OrdinalIgnoreCase))
        {
            var id = Value(query, "id");
            var vendor = id?.StartsWith("AW-", StringComparison.OrdinalIgnoreCase) == true ? TagVendor.GoogleAds : TagVendor.Ga4;
            return new CapturedTagRequest(url, vendor, id, gcs, gcd, beforeConsent);
        }

        if (IsGa4(host, path))
            return new CapturedTagRequest(url, TagVendor.Ga4, First(query, "tid", "measurement_id", "id"), gcs, gcd, beforeConsent);

        if (IsGoogleAds(host, path))
            return new CapturedTagRequest(url, TagVendor.GoogleAds, AdsId(path, query), gcs, gcd, beforeConsent);

        if (IsMeta(host, path))
            return new CapturedTagRequest(url, TagVendor.MetaPixel, Value(query, "id"), gcs, gcd, beforeConsent);

        return null;
    }

    private static bool IsGa4(string host, string path) =>
        (host.Contains("google-analytics.com", StringComparison.OrdinalIgnoreCase) || host.Contains("analytics.google.com", StringComparison.OrdinalIgnoreCase))
        && path.Contains("collect", StringComparison.OrdinalIgnoreCase);

    private static bool IsGoogleAds(string host, string path) =>
        host.Contains("googleadservices.com", StringComparison.OrdinalIgnoreCase)
        || host.Contains("googleads.g.doubleclick.net", StringComparison.OrdinalIgnoreCase)
        || host.Contains("google.", StringComparison.OrdinalIgnoreCase) && path.Contains("/pagead/", StringComparison.OrdinalIgnoreCase);

    private static bool IsMeta(string host, string path) =>
        host.Contains("facebook.com", StringComparison.OrdinalIgnoreCase) && path.Contains("/tr", StringComparison.OrdinalIgnoreCase)
        || host.Contains("facebook.net", StringComparison.OrdinalIgnoreCase);

    private static string? AdsId(string path, Dictionary<string, string> query)
    {
        var fromQuery = First(query, "aw", "label", "id");
        if (fromQuery is not null) return fromQuery;
        var marker = "/conversion/";
        var at = path.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (at < 0) return null;
        var rest = path[(at + marker.Length)..];
        var slash = rest.IndexOf('/');
        var id = slash < 0 ? rest : rest[..slash];
        return id.Length == 0 ? null : id;
    }

    private static string? First(Dictionary<string, string> query, params string[] keys)
    {
        foreach (var key in keys)
            if (Value(query, key) is { } value) return value;
        return null;
    }

    private static string? Value(Dictionary<string, string> query, string key) =>
        query.TryGetValue(key, out var value) && value.Length > 0 ? value : null;

    private static Dictionary<string, string> Query(Uri uri)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var text = uri.Query.TrimStart('?');
        foreach (var part in text.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            var key = Uri.UnescapeDataString(eq < 0 ? part : part[..eq]);
            var value = eq < 0 ? "" : Uri.UnescapeDataString(part[(eq + 1)..]);
            result.TryAdd(key, value);
        }
        return result;
    }
}
