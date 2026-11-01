using MediatR;

namespace MarketingOps.Application.Rendering.Queries.GetRenderScreenshot;

/// <summary>Render kanıt ekran görüntüsünü getirir; yoksa <c>NotFoundException</c>.</summary>
public sealed record GetRenderScreenshotQuery(Guid SiteId, Guid ScreenshotId) : IRequest<RenderScreenshotResponse>;
