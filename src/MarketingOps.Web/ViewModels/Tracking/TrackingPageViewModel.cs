using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Tracking.Queries.GetEventHealth;
using MarketingOps.Application.Tracking.Queries.GetOrderComparison;
using MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;
using MarketingOps.Application.Tracking.Queries.GetTrackingScore;

namespace MarketingOps.Web.ViewModels.Tracking;

public sealed record TrackingPageViewModel(
    SiteDetailResponse Site,
    TrackingScoreResponse Score,
    TagAuditMatrixResponse Matrix,
    EventHealthResponse Events,
    OrderComparisonResponse Orders,
    bool MetaConnected);
