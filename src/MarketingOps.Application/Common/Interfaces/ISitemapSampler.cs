namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Henüz taranmamış bir sitenin sitemap'indeki sayfa adreslerini canlı okur (robots.txt'deki sitemap'ler, yoksa /sitemap.xml).</summary>
public interface ISitemapSampler
{
    /// <summary>Okunamayan sitemap boş liste döndürür; ağ hatası fırlatılmaz.</summary>
    Task<IReadOnlyList<string>> GetUrlsAsync(string domain, CancellationToken cancellationToken);
}
