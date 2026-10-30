using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.IntegrationTests.Crawling;

/// <summary>
/// Ağa çıkmadan taranan sahte site. 20 sayfa: ana sayfa, kategoriler, ürünler, blog; ayrıca robots ile engelli,
/// hariç tutulan, yönlendiren ve 404 dönen adresler.
/// </summary>
public sealed class FakeSite : ICrawlerHttpClient
{
    private readonly Dictionary<string, CrawlerResponse> _pages = [];
    public string Domain { get; }
    public List<string> Requested { get; } = [];

    public FakeSite(string domain)
    {
        Domain = domain;
        var root = $"https://{domain}";

        Text("/robots.txt", $"User-agent: *\nDisallow: /hesabim/\nSitemap: {root}/sitemap.xml");
        Xml("/sitemap.xml", $"""
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
            {string.Concat(Enumerable.Range(1, 12).Select(i => $"<url><loc>{root}/urun/p{i}</loc></url>"))}
            <url><loc>{root}/hesabim/siparisler</loc></url>
            <url><loc>{root}/arama/lastik</loc></url>
            </urlset>
            """);

        Html("/", "Ana sayfa", links: ["/kategori/yaz", "/kategori/kis", "/kategori/suv", "/blog/rehber", "/blog/bakim", "/eski-urun", "/yok-boyle-sayfa", "/hakkimizda"]);
        foreach (var c in new[] { "yaz", "kis", "suv" })
            Html($"/kategori/{c}", $"{c} lastikleri", links: Enumerable.Range(1, 12).Select(i => $"/urun/p{i}").ToArray());
        for (var i = 1; i <= 12; i++) Html($"/urun/p{i}", $"Ürün {i}", schema: "Product");
        Html("/blog/rehber", "Rehber", schema: "Article");
        Html("/blog/bakim", "Bakım", schema: "Article");
        Html("/hakkimizda", "Hakkımızda");
        _pages["/eski-urun"] = new CrawlerResponse(301, new Uri($"{root}/urun/p1"), null, null, null, null, null, 5);
        _pages["/yok-boyle-sayfa"] = new CrawlerResponse(404, null, "text/html", null, null, null, null, 5);
    }

    /// <summary>Ürün sayfalarına ortak bir title verir (null: her ürün kendi adını alır). Bulgu yaşam döngüsü testleri için.</summary>
    public void SetProductTitles(string? shared)
    {
        for (var i = 1; i <= 12; i++) Html($"/urun/p{i}", shared ?? $"Ürün {i}", schema: "Product");
    }

    /// <summary>Ürün sayfalarının head'ine işaretleme ekler (ör. viewport). İşaretleme kuralları testleri için.</summary>
    public void SetProductHead(string head)
    {
        for (var i = 1; i <= 12; i++) Html($"/urun/p{i}", $"Ürün {i}", schema: "Product", head: head);
    }

    /// <summary>Testin araya girmesi: null dışı bir yanıt dönerse sitenin normal yanıtı yerine o kullanılır.</summary>
    public Func<string, CrawlerResponse?>? Intercept { get; set; }

    /// <summary>Her istekten sonra çağrılır (istek sayısıyla); ör. belirli bir noktada işçiyi "kapatmak" için.</summary>
    public Action<int>? AfterRequest { get; set; }

    public Task<CrawlerResponse> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        int count;
        lock (Requested)
        {
            Requested.Add(url.PathAndQuery);
            count = Requested.Count;
        }
        AfterRequest?.Invoke(count);
        cancellationToken.ThrowIfCancellationRequested();
        if (Intercept?.Invoke(url.AbsolutePath) is { } intercepted) return Task.FromResult(intercepted);
        var key = url.AbsolutePath.Length > 1 ? url.AbsolutePath.TrimEnd('/') : url.AbsolutePath;
        return Task.FromResult(_pages.TryGetValue(key, out var response)
            ? response
            : new CrawlerResponse(404, null, "text/html", null, null, null, null, 3));
    }

    private void Html(string path, string title, string? schema = null, string[]? links = null, string head = "")
    {
        var jsonLd = schema is null ? "" : $$"""<script type="application/ld+json">{"@type":"{{schema}}"}</script>""";
        var anchors = string.Concat((links ?? []).Select(l => $"<a href=\"{l}\">{l}</a>"));
        var body = $"<html><head><title>{title}</title>{head}{jsonLd}</head><body><h1>{title}</h1>{anchors}</body></html>";
        _pages[path] = new CrawlerResponse(200, null, "text/html; charset=utf-8", "gzip", "max-age=60", body, body.Length, 40);
    }

    private void Text(string path, string body) => _pages[path] = new CrawlerResponse(200, null, "text/plain", null, null, body, body.Length, 5);

    private void Xml(string path, string body) => _pages[path] = new CrawlerResponse(200, null, "application/xml", null, null, body, body.Length, 5);
}
