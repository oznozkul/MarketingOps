namespace MarketingOps.Application.Rendering.Queries.GetRenderScreenshot;

public sealed record RenderScreenshotResponse(string ContentType, byte[] Bytes);
