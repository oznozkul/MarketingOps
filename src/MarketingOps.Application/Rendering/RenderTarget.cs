using MarketingOps.Domain.Rendering;

namespace MarketingOps.Application.Rendering;

public sealed record RenderTarget(string Url, Guid? PageTypeId, RenderDevice Device);
