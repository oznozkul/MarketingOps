using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Connectors.Commands.SyncSearchConsole;

public sealed class SyncSearchConsoleCommandHandler(IAppDbContext db, GoogleTokenProvider tokens, ISearchConsoleClient client,
    GoogleSyncOptions options, TimeProvider clock, ILogger<SyncSearchConsoleCommandHandler> logger) : IRequestHandler<SyncSearchConsoleCommand>
{
    // Search Console verisi 2–3 gün gecikmeli gelir; son günler eksik olmasın diye bitiş 3 gün öncesidir.
    private const int DataDelayDays = 3;
    private const int WindowDays = 28;

    public async Task Handle(SyncSearchConsoleCommand request, CancellationToken cancellationToken)
    {
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == request.SiteId && a.Type == ConnectorType.SearchConsole
            && a.ExternalAccountId != null && a.ProtectedSecret != null, cancellationToken);
        if (account is null) return;
        var siteUrl = account.ExternalAccountId!;

        try
        {
            var token = await tokens.AccessTokenAsync(account, cancellationToken);
            await SyncPerformanceAsync(request.SiteId, token, siteUrl, cancellationToken);
            await SyncSitemapsAsync(request.SiteId, token, siteUrl, cancellationToken);
            await InspectUrlsAsync(request.SiteId, token, siteUrl, cancellationToken);
            account.RecordSync(clock.GetUtcNow());
        }
        catch (GoogleApiException ex)
        {
            logger.LogWarning("Search Console senkronu başarısız ({SiteId}): {Message}", request.SiteId, ex.Message);
            if (!ex.IsAuthorizationError) account.RecordFailure(ex.Message, authorizationExpired: false, clock.GetUtcNow());
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SyncPerformanceAsync(Guid siteId, string token, string siteUrl, CancellationToken ct)
    {
        var end = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-DataDelayDays);
        var start = end.AddDays(-(WindowDays - 1));
        var rows = await client.QueryPagesAsync(token, siteUrl, start, end, ct);
        var existing = (await db.SearchPerformance.Where(r => r.SiteId == siteId && r.Date >= start && r.Date <= end).ToListAsync(ct))
            .ToDictionary(r => (r.Date, r.Url));
        foreach (var row in rows)
        {
            if (existing.TryGetValue((row.Date, row.Page), out var current)) current.Update(row.Clicks, row.Impressions, row.Ctr, row.Position);
            else db.SearchPerformance.Add(SearchPerformanceDaily.Create(siteId, row.Date, row.Page, row.Clicks, row.Impressions, row.Ctr, row.Position));
        }
    }

    private async Task SyncSitemapsAsync(Guid siteId, string token, string siteUrl, CancellationToken ct)
    {
        var sitemaps = await client.ListSitemapsAsync(token, siteUrl, ct);
        var existing = await db.Sitemaps.Where(s => s.SiteId == siteId).ToListAsync(ct);
        var now = clock.GetUtcNow();
        foreach (var info in sitemaps)
        {
            var status = existing.FirstOrDefault(s => s.Path == info.Path);
            if (status is null) db.Sitemaps.Add(status = SitemapStatus.Create(siteId, info.Path));
            status.Update(info.LastSubmitted, info.LastDownloaded, info.IsPending, info.Errors, info.Warnings, info.Submitted, info.Indexed, now);
        }
        db.Sitemaps.RemoveRange(existing.Where(s => sitemaps.All(i => i.Path != s.Path)));
    }

    private async Task InspectUrlsAsync(Guid siteId, string token, string siteUrl, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var recent = now.AddDays(-options.UrlInspectionRefreshDays);
        var inspected = await db.UrlIndexStatuses.Where(s => s.SiteId == siteId).ToDictionaryAsync(s => s.Url, ct);
        var candidates = await CandidatesAsync(siteId, ct);
        var due = candidates.Where(url => !inspected.TryGetValue(url, out var s) || s.InspectedAt < recent).Take(options.UrlInspectionDailyLimit);

        foreach (var url in due)
        {
            var result = await client.InspectUrlAsync(token, siteUrl, url, ct);
            if (!inspected.TryGetValue(url, out var status)) db.UrlIndexStatuses.Add(status = UrlIndexStatus.Create(siteId, url));
            status.Update(result.Verdict, result.CoverageState, result.IndexingState, result.LastCrawlTime, now);
        }
    }

    /// <summary>Öncelik sırası: açık bulgusu olan sayfalar, en çok gösterim alan sayfalar, son taramadaki diğer sayfalar.</summary>
    private async Task<IReadOnlyList<string>> CandidatesAsync(Guid siteId, CancellationToken ct)
    {
        var crawlId = await db.Crawls.Where(c => c.SiteId == siteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .OrderByDescending(c => c.Number).Select(c => (Guid?)c.Id).FirstOrDefaultAsync(ct);
        if (crawlId is null) return [];
        // Yönlendirme zinciri JSON kolonunda; filtre bellekte.
        var crawled = (await db.PageSnapshots.Where(p => p.CrawlId == crawlId && p.StatusCode >= 200 && p.StatusCode < 300)
                .Select(p => new { p.Url, p.RedirectChain }).ToListAsync(ct))
            .Where(p => p.RedirectChain.Count == 0).Select(p => p.Url).ToList();
        var crawledSet = crawled.ToHashSet();

        var withFindings = (await db.Findings.AsNoTracking().Where(f => f.SiteId == siteId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
                .Select(f => f.Occurrences).ToListAsync(ct))
            .SelectMany(o => o).Select(o => o.Url).Where(crawledSet.Contains);
        var since = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-WindowDays - DataDelayDays);
        var popular = await db.SearchPerformance.Where(r => r.SiteId == siteId && r.Date >= since)
            .GroupBy(r => r.Url).OrderByDescending(g => g.Sum(r => r.Impressions)).Select(g => g.Key).Take(500).ToListAsync(ct);

        return withFindings.Concat(popular.Where(crawledSet.Contains)).Concat(crawled).Distinct().ToList();
    }
}
