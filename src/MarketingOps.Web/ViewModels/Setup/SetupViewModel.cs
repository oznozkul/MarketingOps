using MarketingOps.Application.Connectors.Queries.GetConnectors;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Sites;
using MarketingOps.Web.ViewModels.Sites;

namespace MarketingOps.Web.ViewModels.Setup;

/// <summary>Sihirbazın bir adımı. Adıma özgü alanlar yalnızca o adımda doludur.</summary>
public sealed record SetupViewModel(SiteDetailResponse Site, SiteSetupStep Step)
{
    /// <summary>Kurulumda sıradaki adım; kurulum bittiyse null (adımlar yine gezilebilir).</summary>
    public SiteSetupStep? Current => Site.SetupStep;

    public bool IsReached(SiteSetupStep step) => Current is null || step <= Current;

    public ConnectorsResponse? Connectors { get; init; }
    public TrackingIdsForm? TrackingIds { get; init; }
    public MetaTokenForm? Meta { get; init; }
    public CrawlSettingsForm? Crawl { get; init; }
}
