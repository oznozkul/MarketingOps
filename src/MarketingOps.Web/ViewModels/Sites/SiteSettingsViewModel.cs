using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Sites.Queries.GetSites;
using MarketingOps.Application.Tasks.Queries.GetAiStatus;

namespace MarketingOps.Web.ViewModels.Sites;

public sealed record SiteSettingsViewModel(
    SiteDetailResponse Site,
    IReadOnlyList<SiteListItemResponse> Sites,
    SiteNameForm NameForm,
    CrawlSettingsForm CrawlForm,
    TrackingIdsForm TrackingForm,
    TaskKeyPrefixForm TaskKeyForm,
    TaskTrackerForm TaskTrackerForm,
    AiStatusResponse? Ai);
