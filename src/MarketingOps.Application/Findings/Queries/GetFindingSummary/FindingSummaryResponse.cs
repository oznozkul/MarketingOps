namespace MarketingOps.Application.Findings.Queries.GetFindingSummary;

/// <summary><see cref="HasAnyFinding"/>: filtreden bağımsız olarak sitede hiç bulgu var mı (olumlu boş durum için).</summary>
public sealed record FindingSummaryResponse(int Total, int Critical, int NewInLastCrawl, bool HasAnyFinding, bool HasEvaluatedCrawl);
