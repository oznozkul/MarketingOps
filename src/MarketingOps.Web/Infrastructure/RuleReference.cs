namespace MarketingOps.Web.Infrastructure;

/// <summary>Kuralın referans anahtarının okunur karşılığı: iç performans dokümanının sayfası ya da Google dokümantasyonu.</summary>
public sealed record RuleReference(string Label, string? Url)
{
    private static readonly Dictionary<string, (string Label, string Url)> Google = new()
    {
        ["canonical"] = ("Yinelenen URL'leri birleştirme", "https://developers.google.com/search/docs/crawling-indexing/consolidate-duplicate-urls"),
        ["headings"] = ("SEO başlangıç kılavuzu", "https://developers.google.com/search/docs/fundamentals/seo-starter-guide"),
        ["helpful-content"] = ("Faydalı içerik oluşturma", "https://developers.google.com/search/docs/fundamentals/creating-helpful-content"),
        ["hreflang"] = ("Yerelleştirilmiş sürümler (hreflang)", "https://developers.google.com/search/docs/specialty/international/localized-versions"),
        ["images"] = ("Google Görseller için en iyi uygulamalar", "https://developers.google.com/search/docs/appearance/google-images"),
        ["links"] = ("Taranabilir bağlantılar", "https://developers.google.com/search/docs/crawling-indexing/links-crawlable"),
        ["redirects"] = ("Yönlendirmeler ve Google Arama", "https://developers.google.com/search/docs/crawling-indexing/301-redirects"),
        ["robots"] = ("Robots meta etiketi ve X-Robots-Tag", "https://developers.google.com/search/docs/crawling-indexing/robots-meta-tag"),
        ["sitemaps"] = ("Site haritaları", "https://developers.google.com/search/docs/crawling-indexing/sitemaps/overview"),
        ["snippets"] = ("Arama sonucu snippet'leri", "https://developers.google.com/search/docs/appearance/snippet"),
        ["structured-data"] = ("Yapılandırılmış veri", "https://developers.google.com/search/docs/appearance/structured-data/intro-structured-data"),
        ["title-links"] = ("Arama sonuçlarındaki başlık bağlantıları", "https://developers.google.com/search/docs/appearance/title-link")
    };

    public static RuleReference Of(string key)
    {
        var (source, value) = key.Split(':', 2) is [var s, var v] ? (s, v) : (key, "");
        if (source == "perf-doc" && value.StartsWith('p'))
            return new RuleReference($"Web Sitesi Performans Optimizasyonu, s. {value[1..]}", null);
        if (source == "google" && Google.TryGetValue(value, out var doc))
            return new RuleReference($"Google Search Central: {doc.Label}", doc.Url);
        return new RuleReference(key, null);
    }
}
