namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record ExpectedTrackingIdsResponse(
    string? Ga4MeasurementId,
    string? GtmContainerId,
    string? MetaPixelId,
    string? GoogleAdsConversionId);
