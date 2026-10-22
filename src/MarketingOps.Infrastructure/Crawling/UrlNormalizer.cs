namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Aynı sayfanın farklı yazımlarını tek anahtara indirger: fragment atılır, şema/host küçültülür, varsayılan port
/// kaldırılır, query parametreleri sıralanır, kök dışında sondaki "/" kaldırılır.
/// </summary>
public static class UrlNormalizer
{
    public static string Normalize(Uri url)
    {
        var builder = new UriBuilder(url)
        {
            Scheme = url.Scheme.ToLowerInvariant(),
            Host = url.Host.ToLowerInvariant(),
            Fragment = ""
        };
        if (url.IsDefaultPort) builder.Port = -1;

        var path = builder.Path;
        if (path.Length > 1 && path.EndsWith('/')) path = path.TrimEnd('/');
        builder.Path = path.Length == 0 ? "/" : path;

        var query = url.Query.TrimStart('?');
        builder.Query = query.Length == 0
            ? ""
            : string.Join("&", query.Split('&', StringSplitOptions.RemoveEmptyEntries).OrderBy(p => p, StringComparer.Ordinal));

        return builder.Uri.AbsoluteUri;
    }

    /// <summary>Sayfadaki href'i mutlak http(s) URL'e çevirir; mailto, tel, javascript gibi bağlantılar için null.</summary>
    public static Uri? Resolve(Uri baseUrl, string? href)
    {
        if (string.IsNullOrWhiteSpace(href)) return null;
        if (!Uri.TryCreate(baseUrl, href.Trim(), out var url)) return null;
        return url.Scheme is "http" or "https" ? url : null;
    }
}
