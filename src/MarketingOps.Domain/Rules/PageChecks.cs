using MarketingOps.Domain.Crawls;

namespace MarketingOps.Domain.Rules;

/// <summary>Kuralların ortak sayfa koşulları.</summary>
public static class PageChecks
{
    /// <summary>Doğrudan (yönlendirmesiz) 2xx dönen, içeriği ayrıştırılmış HTML sayfa.</summary>
    public static bool IsHtml(PageSnapshot page) =>
        page.StatusCode is >= 200 and < 300
        && page.RedirectChain.Count == 0
        && (page.ContentType is null || page.ContentType.Contains("html", StringComparison.OrdinalIgnoreCase));

    public static bool IsNoindex(PageSnapshot page) =>
        page.Data.MetaRobots?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>Snapshot Faz 8'in işaretleme alanlarını taşıyor; eski kayıtlarda bu alanlar boştur ve yanlış bulgu üretir.</summary>
    public static bool HasMarkupFacts(PageSnapshot page) => page.Data.Version >= PageSnapshotData.MarkupVersion;

    /// <summary>Sitenin kök adresi (sorgu dizesi olmadan "/").</summary>
    public static bool IsHomePage(PageSnapshot page) =>
        Uri.TryCreate(page.Url, UriKind.Absolute, out var url) && url.AbsolutePath == "/" && url.Query.Length == 0;

    /// <summary>Arama sonuçlarında görünmesi beklenen sayfa: HTML ve noindex değil.</summary>
    public static bool IsIndexable(PageSnapshot page) => IsHtml(page) && !IsNoindex(page);
}
