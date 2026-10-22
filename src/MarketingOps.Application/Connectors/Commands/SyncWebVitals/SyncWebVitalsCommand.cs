using MediatR;

namespace MarketingOps.Application.Connectors.Commands.SyncWebVitals;

/// <summary>
/// Son taramadan her sayfa tipi için örnek sayfaları PageSpeed Insights ile mobil ve masaüstünde ölçer (bugünün kaydı, upsert).
/// Ölçümü Google yapar; site MarketingOps tarafından ayrıca istenmez.
/// </summary>
public sealed record SyncWebVitalsCommand(Guid SiteId) : IRequest;
