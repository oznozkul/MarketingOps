using MarketingOps.Application.Crawls.Queries.GetCrawlStatus;
using MarketingOps.Application.Notifications.Queries.GetNotifications;
using MarketingOps.Application.Sites.Queries.GetSites;

namespace MarketingOps.Web.ViewModels;

/// <summary>Layout'un (menü, üst bar) ihtiyaç duyduğu veriler. Her istekte SiteContextFilter üretir.</summary>
public sealed record ShellViewModel(
    SiteListItemResponse Site,
    IReadOnlyList<SiteSwitchItem> Sites,
    CrawlStatusResponse CrawlStatus,
    string ActiveSection,
    string UserName,
    string UserRole,
    NotificationsResponse Notifications);
