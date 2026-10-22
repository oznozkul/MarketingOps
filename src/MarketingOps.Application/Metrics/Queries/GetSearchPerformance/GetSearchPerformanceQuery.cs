using MediatR;

namespace MarketingOps.Application.Metrics.Queries.GetSearchPerformance;

/// <summary>
/// Son 28 günün Search Console toplamı: <see cref="FindingId"/> verilirse bulgunun etkilenen sayfaları için, yoksa sitenin tamamı için.
/// Hiç veri yoksa null.
/// </summary>
public sealed record GetSearchPerformanceQuery(Guid SiteId, Guid? FindingId = null) : IRequest<SearchPerformanceResponse?>;
