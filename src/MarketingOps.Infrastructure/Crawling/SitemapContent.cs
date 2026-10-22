namespace MarketingOps.Infrastructure.Crawling;

/// <summary>Bir sitemap dosyasının içeriği: sayfa URL'leri ve (sitemap index ise) alt sitemap'ler.</summary>
public sealed record SitemapContent(IReadOnlyList<string> Urls, IReadOnlyList<string> Sitemaps);
