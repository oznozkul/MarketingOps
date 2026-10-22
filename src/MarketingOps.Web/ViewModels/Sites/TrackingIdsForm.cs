using MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;
using MarketingOps.Application.Sites.Queries.GetSiteById;

namespace MarketingOps.Web.ViewModels.Sites;

public sealed class TrackingIdsForm
{
    public string? Ga4MeasurementId { get; set; }
    public string? GtmContainerId { get; set; }
    public string? MetaPixelId { get; set; }
    public string? GoogleAdsConversionId { get; set; }

    public static TrackingIdsForm From(ExpectedTrackingIdsResponse ids) => new()
    {
        Ga4MeasurementId = ids.Ga4MeasurementId,
        GtmContainerId = ids.GtmContainerId,
        MetaPixelId = ids.MetaPixelId,
        GoogleAdsConversionId = ids.GoogleAdsConversionId
    };

    public UpdateExpectedTrackingIdsCommand ToCommand(Guid siteId) =>
        new(siteId, Ga4MeasurementId, GtmContainerId, MetaPixelId, GoogleAdsConversionId);
}
