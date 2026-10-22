using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Organizations;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
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
    DbSet<Finding> Findings { get; }
    DbSet<SiteRuleSetting> SiteRuleSettings { get; }
    DbSet<WorkTask> WorkTasks { get; }
    DbSet<TaskTrackerConnection> TaskTrackerConnections { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
