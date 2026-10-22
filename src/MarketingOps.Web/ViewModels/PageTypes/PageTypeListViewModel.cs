using MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;
using MarketingOps.Application.Sites.Queries.GetSiteById;

namespace MarketingOps.Web.ViewModels.PageTypes;

public sealed record PageTypeListViewModel(SiteDetailResponse Site, PageTypeStatsResponse Stats);
