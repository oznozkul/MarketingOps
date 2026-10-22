using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetUntaskedFindingCount;

/// <summary>Görevi olmayan açık bulgu sayısı (görev üretiminden önce açılmış ya da elle oluşturulmamış bulgular).</summary>
public sealed record GetUntaskedFindingCountQuery(Guid SiteId) : IRequest<int>;
