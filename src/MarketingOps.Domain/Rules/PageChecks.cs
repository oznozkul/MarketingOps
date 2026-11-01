using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rendering;

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
        page.Data.MetaRobots?.Contains("noindex", StringComparison.OrdinalIgnoreCase) == true
        || RobotsHeaderHasNoindex(page.Data.RobotsHeader);

    /// <summary>Snapshot Faz 8'in işaretleme alanlarını taşıyor; eski kayıtlarda bu alanlar boştur ve yanlış bulgu üretir.</summary>
    public static bool HasMarkupFacts(PageSnapshot page) => page.Data.Version >= PageSnapshotData.MarkupVersion;

    /// <summary>Snapshot hreflang ve X-Robots-Tag alanlarını taşıyor.</summary>
    public static bool HasSignals(PageSnapshot page) => page.Data.Version >= PageSnapshotData.SignalsVersion;

    /// <summary>Sitenin kök adresi (sorgu dizesi olmadan "/").</summary>
    public static bool IsHomePage(PageSnapshot page) =>
        Uri.TryCreate(page.Url, UriKind.Absolute, out var url) && url.AbsolutePath == "/" && url.Query.Length == 0;

    /// <summary>Arama sonuçlarında görünmesi beklenen sayfa: HTML ve noindex değil.</summary>
    public static bool IsIndexable(PageSnapshot page) => IsHtml(page) && !IsNoindex(page);

    /// <summary>
    /// HTML kuralının bulduğu işaretleme sorununun render verisiyle bastırılıp bastırılmayacağı. Sayfa tipinin ölçülmüş
    /// örnekleri varsa ve hiçbir örnekte görünür (CSS ile gizlenmemiş) sorunlu öğe yoksa, HTML'deki sorun gizli kopyadan
    /// geliyordur; bulgu açılmaz. Ölçüm sürümü görünürlük sayımlarını taşımayan eski veriyle bastırma yapılmaz.
    /// </summary>
    public static bool RenderSuppressesHtmlFinding(RuleContext context, Guid? pageTypeId, Func<RenderMeasurements, int> visibleCount)
    {
        var samples = context.External.Renders
            .Where(r => r.PageTypeId == pageTypeId && r.Measurements.HasVisibilityCounts)
            .ToList();
        return samples.Count > 0 && samples.All(r => visibleCount(r.Measurements) == 0);
    }

    /// <summary>
    /// X-Robots-Tag başlığı noindex içeriyor mu ("none" = noindex). "googlebot: noindex" gibi Google'a özel yönerge
    /// sayılır; başka bir bota özel yönerge ("bingbot: noindex") Google'ın dizinini etkilemez, sayılmaz.
    /// </summary>
    public static bool RobotsHeaderHasNoindex(string? header) =>
        header is not null && header.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(DirectiveForGoogle)
            .Any(token => token is not null && (token.Equals("noindex", StringComparison.OrdinalIgnoreCase)
                                                || token.Equals("none", StringComparison.OrdinalIgnoreCase)));

    private static string? DirectiveForGoogle(string token)
    {
        var colon = token.IndexOf(':');
        if (colon < 0) return token.Trim();
        var agent = token[..colon].Trim();
        // "unavailable_after: <tarih>" bot adı değil yönergedir; noindex olmadığı için sonucu etkilemez.
        return agent.Equals("googlebot", StringComparison.OrdinalIgnoreCase) ? token[(colon + 1)..].Trim() : null;
    }
}
