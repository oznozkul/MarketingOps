using MediatR;

namespace MarketingOps.Application.Rendering.Queries.GetRenderScreenshot;

/// <summary>Render kanıt ekran görüntüsünü getirir; yoksa null döner (controller 404 üretir).</summary>
public sealed record GetRenderScreenshotQuery(Guid SiteId, Guid ScreenshotId) : IRequest<RenderScreenshotResponse?>;
