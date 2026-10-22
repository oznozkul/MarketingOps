namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Tarayıcının URL anahtarıyla aynı normalizasyon (fragment yok, kök dışında sondaki "/" yok, query sıralı).
/// Kırık bağlantı kuralı, sayfadaki linkleri hata alan sayfaların anahtarlarıyla karşılaştırırken kullanır.
/// </summary>
public static class UrlKey
{
    public static string Normalize(Uri url)
    {
        var builder = new UriBuilder(url) { Scheme = url.Scheme.ToLowerInvariant(), Host = url.Host.ToLowerInvariant(), Fragment = "" };
        if (url.IsDefaultPort) builder.Port = -1;
        var path = builder.Path.Length > 1 ? builder.Path.TrimEnd('/') : builder.Path;
        builder.Path = path.Length == 0 ? "/" : path;
        var query = url.Query.TrimStart('?');
        builder.Query = query.Length == 0 ? "" : string.Join("&", query.Split('&', StringSplitOptions.RemoveEmptyEntries).OrderBy(p => p, StringComparer.Ordinal));
        return builder.Uri.AbsoluteUri;
    }
}
