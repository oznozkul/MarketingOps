using MarketingOps.Application.PageTypes;
using MarketingOps.Application.PageTypes.Commands.AddPageType;
using MarketingOps.Application.PageTypes.Commands.UpdatePageType;
using MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Sites;

namespace MarketingOps.Web.ViewModels.PageTypes;

/// <summary>Sayfa tipi ekleme/düzenleme formu. Örnek URL'ler yalnızca önizleme içindir, kaydedilmez.</summary>
public sealed class PageTypeForm
{
    private const int MaxSamples = 200;

    public string Name { get; set; } = "";
    public MatchMode Mode { get; set; } = MatchMode.Any;
    public List<MatchConditionInput> Conditions { get; set; } = [];
    public string? SampleUrls { get; set; }

    public static PageTypeForm From(PageTypeResponse pageType) => new()
    {
        Name = pageType.Name,
        Mode = pageType.Mode,
        Conditions = pageType.Conditions.Select(c => new MatchConditionInput(c.Type, c.Value)).ToList()
    };

    public AddPageTypeCommand ToAddCommand(Guid siteId) => new(siteId, Name, Mode, Conditions);

    public UpdatePageTypeCommand ToUpdateCommand(Guid siteId, Guid pageTypeId) => new(siteId, pageTypeId, Name, Mode, Conditions);

    public PreviewPageTypeMatchQuery ToPreviewQuery(Guid siteId, Guid? pageTypeId) => new(
        Mode,
        Conditions,
        (SampleUrls ?? "").Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .Take(MaxSamples)
            .Select(url => new PreviewSampleInput(url))
            .ToList(),
        siteId,
        pageTypeId);

    /// <summary>
    /// Henüz tarama verisi yokken önizleme için örnek URL'ler: sitenin mevcut glob desenlerinden türetilir.
    /// Tarama sonrası gerçek sayfalar kullanılacak.
    /// </summary>
    public static string DefaultSamples(string domain, IEnumerable<PageTypeResponse> pageTypes)
    {
        var paths = pageTypes
            .SelectMany(p => p.Conditions)
            .Where(c => c.Type == MatchConditionType.UrlGlob)
            .Select(c => c.Value.Replace("**", "ornek/alt-sayfa").Replace("*", "ornek"))
            .Prepend("/")
            .Distinct()
            .Take(12);
        return string.Join("\n", paths.Select(p => $"https://{domain}{(p.StartsWith('/') ? p : "/" + p)}"));
    }
}
