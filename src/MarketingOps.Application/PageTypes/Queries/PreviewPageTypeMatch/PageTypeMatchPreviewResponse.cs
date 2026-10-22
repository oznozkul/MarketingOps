namespace MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;

public sealed record PageTypeMatchPreviewResponse(int TotalCount, int MatchedCount, int OverlapCount, IReadOnlyList<UrlMatchResponse> Results);
