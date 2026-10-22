namespace MarketingOps.Application.Tracking;

/// <summary>Temsilci sayfaları açıp tag isteklerini toplar. Testler Chromium açmadan sahte istemci verir.</summary>
public interface ITrackingBrowser
{
    Task<IReadOnlyList<PageObservation>> ProbeAsync(IReadOnlyList<string> urls, string? consentSelector, CancellationToken cancellationToken);
}
