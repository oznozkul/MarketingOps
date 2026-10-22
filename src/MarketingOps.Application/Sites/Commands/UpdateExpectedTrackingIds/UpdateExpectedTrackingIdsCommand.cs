using MediatR;

namespace MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;

public sealed record UpdateExpectedTrackingIdsCommand(
    Guid SiteId,
    string? Ga4MeasurementId,
    string? GtmContainerId,
    string? MetaPixelId,
    string? GoogleAdsConversionId) : IRequest;
