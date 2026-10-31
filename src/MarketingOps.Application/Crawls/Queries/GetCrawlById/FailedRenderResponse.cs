using MarketingOps.Domain.Rendering;

namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record FailedRenderResponse(string Url, RenderDevice Device, string? Error);
