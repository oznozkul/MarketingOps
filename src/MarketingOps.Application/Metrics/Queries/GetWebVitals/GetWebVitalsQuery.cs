using MarketingOps.Domain.Metrics;
using MediatR;

namespace MarketingOps.Application.Metrics.Queries.GetWebVitals;

/// <summary>Sayfa tipi bazında en güncel Core Web Vitals ve son 28 günlük trend (günlük ortanca).</summary>
public sealed record GetWebVitalsQuery(Guid SiteId, DeviceStrategy Strategy) : IRequest<WebVitalsResponse>;
