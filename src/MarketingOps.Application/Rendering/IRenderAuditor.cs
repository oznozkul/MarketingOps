namespace MarketingOps.Application.Rendering;

/// <summary>Örnek sayfaları gerçek tarayıcıda cihaz öykünmesiyle açıp ölçer. Testler Chromium açmadan sahte ölçüm verir.</summary>
public interface IRenderAuditor
{
    Task<IReadOnlyList<RenderResult>> AuditAsync(IReadOnlyList<RenderTarget> targets, RenderOptions options, CancellationToken cancellationToken);
}
