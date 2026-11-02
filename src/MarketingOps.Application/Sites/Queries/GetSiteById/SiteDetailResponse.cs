using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record SiteDetailResponse(
    Guid Id,
    string Name,
    string Domain,
    bool IsOwnershipVerified,
    DateTimeOffset CreatedAt,
    CrawlSettingsResponse CrawlSettings,
    ExpectedTrackingIdsResponse ExpectedTrackingIds,
    IReadOnlyList<PageTypeResponse> PageTypes,
    string TaskKeyPrefix,
    TrackingJourneyResponse Journey,
    OrderSourceResponse OrderSource,
    SiteSetupStep? SetupStep);
