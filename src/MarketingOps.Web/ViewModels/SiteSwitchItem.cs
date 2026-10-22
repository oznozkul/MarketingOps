using MarketingOps.Application.Sites.Queries.GetSites;

namespace MarketingOps.Web.ViewModels;

public sealed record SiteSwitchItem(SiteListItemResponse Site, string Url, bool IsCurrent);
