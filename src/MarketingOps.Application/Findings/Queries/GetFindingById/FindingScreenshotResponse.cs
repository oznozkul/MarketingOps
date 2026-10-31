using MarketingOps.Domain.Rendering;

namespace MarketingOps.Application.Findings.Queries.GetFindingById;

/// <summary>Render bulgusunun bir örneğine ait kanıt ekran görüntüsü.</summary>
public sealed record FindingScreenshotResponse(Guid Id, RenderDevice Device);
