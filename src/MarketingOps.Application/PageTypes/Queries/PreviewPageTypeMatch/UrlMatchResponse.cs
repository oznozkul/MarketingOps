namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

/// <summary><see cref="OverlappingPageTypes"/>: aynı URL'e uyan, sitede kayıtlı diğer sayfa tiplerinin adları.</summary>
public sealed record UrlMatchResponse(string Url, bool IsMatch, IReadOnlyList<string> OverlappingPageTypes);
