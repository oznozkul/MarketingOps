using MediatR;

namespace MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;

/// <summary>Son başarılı taramaya göre sayfa tipi başına eşleşen sayfa sayısı.</summary>
public sealed record GetPageTypeStatsQuery(Guid SiteId) : IRequest<PageTypeStatsResponse>;
