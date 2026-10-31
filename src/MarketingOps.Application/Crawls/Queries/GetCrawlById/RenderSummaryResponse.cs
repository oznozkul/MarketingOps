namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

/// <summary>Render aşamasının sonucu: ölçülen, ekranı kaplanmış (çerez penceresi) ve ölçülemeyen sayfa × cihaz sayıları.</summary>
public sealed record RenderSummaryResponse(int Measured, int Obstructed, int Failed, IReadOnlyList<FailedRenderResponse> Failures);
