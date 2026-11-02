namespace MarketingOps.Application.PageTypes.Queries.SuggestPageTypes;

public enum PageTypeSuggestionSource
{
    /// <summary>Henüz taranmamış site: sitemap'teki adresler canlı okunur.</summary>
    Sitemap,

    /// <summary>Son tamamlanan taramada hiçbir sayfa tipine uymayan HTML sayfalar.</summary>
    UnmatchedPages
}
