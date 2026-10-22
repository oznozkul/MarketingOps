using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Metrics;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>PageSpeed Insights: önce sayfanın gerçek kullanıcı verisi (CrUX), yoksa sitenin geneli, o da yoksa laboratuvar ölçümü.</summary>
public interface IPageSpeedClient
{
    Task<PageSpeedResult> RunAsync(string url, DeviceStrategy strategy, CancellationToken cancellationToken);
}
