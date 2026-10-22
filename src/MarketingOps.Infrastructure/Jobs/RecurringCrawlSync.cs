using Hangfire;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>
/// İşçi açılırken tüm sitelerin tekrarlayan tarama işlerini kaydeder. Seed ya da elle eklenen veriler için de
/// zamanlamanın garanti olması bu sayede. Veritabanı henüz hazır değilse (ilk açılış, migration) bir süre tekrar dener.
/// </summary>
public sealed class RecurringCrawlSync(IServiceScopeFactory scopes, ILogger<RecurringCrawlSync> logger) : BackgroundService
{
    private const int MaxAttempts = 30;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (var attempt = 1; attempt <= MaxAttempts && !stoppingToken.IsCancellationRequested; attempt++)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var scheduler = scope.ServiceProvider.GetRequiredService<ICrawlScheduler>();
                var sites = await db.Sites.AsNoTracking().Select(s => new { s.Id, s.CrawlSettings.Schedule }).ToListAsync(stoppingToken);
                foreach (var site in sites) scheduler.ScheduleSite(site.Id, site.Schedule);
                var recurring = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
                var istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
                recurring.AddOrUpdate<SnapshotRetentionJob>(
                    SnapshotRetentionJob.RecurringId, job => job.RunAsync(CancellationToken.None), "0 6 * * *",
                    new RecurringJobOptions { TimeZone = istanbul });
                recurring.AddOrUpdate<SendDueReportsJob>(
                    SendDueReportsJob.RecurringId, job => job.RunAsync(CancellationToken.None), "*/15 * * * *",
                    new RecurringJobOptions { TimeZone = istanbul });
                logger.LogInformation("{Count} site için tekrarlayan tarama işi kaydedildi.", sites.Count);
                return;
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Tekrarlayan işler kaydedilemedi (deneme {Attempt}): {Message}", attempt, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}
