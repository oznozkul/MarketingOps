using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Scores;
using MarketingOps.Domain.Organizations;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.Tracking;
using MarketingOps.Domain.WorkTasks;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Handler'lar veritabanına bu arayüz üzerinden erişir. Repository/UoW katmanı yoktur.</summary>
public interface IAppDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<Site> Sites { get; }
    DbSet<Crawl> Crawls { get; }
    DbSet<PageSnapshot> PageSnapshots { get; }
    DbSet<RenderSample> RenderSamples { get; }
    DbSet<RenderScreenshot> RenderScreenshots { get; }
    DbSet<Finding> Findings { get; }
    DbSet<SiteRuleSetting> SiteRuleSettings { get; }
    DbSet<WorkTask> WorkTasks { get; }
    DbSet<ConnectorAccount> ConnectorAccounts { get; }
    DbSet<SearchPerformanceDaily> SearchPerformance { get; }
    DbSet<Ga4EventDaily> Ga4Events { get; }
    DbSet<WebVitalsDaily> WebVitals { get; }
    DbSet<UrlIndexStatus> UrlIndexStatuses { get; }
    DbSet<SitemapStatus> Sitemaps { get; }
    DbSet<TagAuditRun> TagAuditRuns { get; }
    DbSet<TagAuditResult> TagAuditResults { get; }
    DbSet<OrderComparisonDaily> OrderComparisons { get; }
    DbSet<ScoreSnapshot> ScoreSnapshots { get; }
    DbSet<NotificationRule> NotificationRules { get; }
    DbSet<NotificationChannelConfig> NotificationChannels { get; }
    DbSet<NotificationDelivery> NotificationDeliveries { get; }
    DbSet<UserNotification> UserNotifications { get; }
    DbSet<ReportSchedule> ReportSchedules { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
