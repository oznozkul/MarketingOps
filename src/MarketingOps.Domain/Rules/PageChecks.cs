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

    /// <summary>Arama sonuçlarında görünmesi beklenen sayfa: HTML ve noindex değil.</summary>
    public static bool IsIndexable(PageSnapshot page) => IsHtml(page) && !IsNoindex(page);
}
