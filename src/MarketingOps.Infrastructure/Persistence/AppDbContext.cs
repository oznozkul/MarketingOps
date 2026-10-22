using MarketingOps.Application.Common.Events;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Organizations;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options, IPublisher publisher) : DbContext(options), IAppDbContext
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Crawl> Crawls => Set<Crawl>();
    public DbSet<PageSnapshot> PageSnapshots => Set<PageSnapshot>();
    public DbSet<Finding> Findings => Set<Finding>();
    public DbSet<SiteRuleSetting> SiteRuleSettings => Set<SiteRuleSetting>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<ConnectorAccount> ConnectorAccounts => Set<ConnectorAccount>();
    public DbSet<SearchPerformanceDaily> SearchPerformance => Set<SearchPerformanceDaily>();
    public DbSet<Ga4EventDaily> Ga4Events => Set<Ga4EventDaily>();
    public DbSet<WebVitalsDaily> WebVitals => Set<WebVitalsDaily>();
    public DbSet<UrlIndexStatus> UrlIndexStatuses => Set<UrlIndexStatus>();
    public DbSet<SitemapStatus> Sitemaps => Set<SitemapStatus>();

    /// <summary>
    /// Domain olayları kayıt başarılı olduktan sonra yayınlanır; dinleyiciler kalıcı olmuş veriyi görür.
    /// Bir dinleyicinin hatası kaydı geri almaz.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entities = ChangeTracker.Entries<Entity>().Select(e => e.Entity).Where(e => e.DomainEvents.Count > 0).ToList();
        var events = entities.SelectMany(e => e.DomainEvents).ToList();
        foreach (var entity in entities) entity.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);
        foreach (var domainEvent in events)
        {
            var notification = Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)!;
            await publisher.Publish(notification, cancellationToken);
        }
        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
}
