using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record CrawlSettingsResponse(
    CrawlSchedule Schedule,
    int MaxRequestsPerSecond,
    int MaxPages,
    IReadOnlyList<string> ExcludedPathPatterns,
    bool RespectRobotsTxt,
    bool RenderEnabled,
    int RenderSamplesPerPageType);
