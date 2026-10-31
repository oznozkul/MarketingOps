using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules;

/// <summary>
/// Kural motoru için Search Console, URL Inspection, PageSpeed ve değerlendirilen taramanın render verisini yükler. Bir kaynağın verisi eskiyse (bağlantı kopmuş,
/// senkron çalışmamış) kaynak "yok" sayılır; o kaynağın kuralları çalışmaz ve mevcut bulguları değişmez.
/// </summary>
public sealed class ExternalDataLoader(IAppDbContext db, TimeProvider clock)
{
    private const int SearchWindowDays = 28;
    private static readonly TimeSpan MaxSearchAge = TimeSpan.FromDays(7);
    private static readonly TimeSpan MaxInspectionAge = TimeSpan.FromDays(30);
    private static readonly TimeSpan MaxVitalsAge = TimeSpan.FromDays(14);

    public async Task<ExternalData> LoadAsync(Guid siteId, Guid crawlId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var available = new HashSet<RuleDataSource> { RuleDataSource.Crawl };

        var searchByUrl = new Dictionary<string, SearchMetrics>();
        var scConnected = await db.ConnectorAccounts.AnyAsync(a => a.SiteId == siteId && a.Type == ConnectorType.SearchConsole && a.Status == ConnectorStatus.Connected, ct);
        var lastSearchDate = await db.SearchPerformance.Where(r => r.SiteId == siteId).MaxAsync(r => (DateOnly?)r.Date, ct);
        if (scConnected && lastSearchDate is { } last && last >= today.AddDays(-(int)MaxSearchAge.TotalDays - 3))
        {
            available.Add(RuleDataSource.SearchConsole);
            var from = last.AddDays(-(SearchWindowDays - 1));
            var rows = await db.SearchPerformance.Where(r => r.SiteId == siteId && r.Date >= from)
                .GroupBy(r => r.Url)
                .Select(g => new { Url = g.Key, Clicks = g.Sum(r => r.Clicks), Impressions = g.Sum(r => r.Impressions), Weighted = g.Sum(r => r.Position * r.Impressions) })
                .ToListAsync(ct);
            foreach (var r in rows.Where(r => r.Impressions > 0))
                if (Key(r.Url) is { } key) searchByUrl[key] = new SearchMetrics(r.Clicks, r.Impressions, r.Weighted / r.Impressions);
        }

        var indexByUrl = new Dictionary<string, IndexInfo>();
        var inspectedSince = now - MaxInspectionAge;
        var inspections = await db.UrlIndexStatuses.Where(s => s.SiteId == siteId && s.InspectedAt >= inspectedSince)
            .Select(s => new { s.Url, s.Verdict, s.CoverageState }).ToListAsync(ct);
        if (inspections.Count > 0)
        {
            available.Add(RuleDataSource.UrlInspection);
            foreach (var s in inspections)
                if (Key(s.Url) is { } key) indexByUrl[key] = new IndexInfo(s.Verdict, s.CoverageState);
        }

        var vitals = new List<VitalsSample>();
        var lastVitals = await db.WebVitals.Where(v => v.SiteId == siteId).MaxAsync(v => (DateOnly?)v.Date, ct);
        if (lastVitals is { } vitalsDate && vitalsDate >= today.AddDays(-(int)MaxVitalsAge.TotalDays))
        {
            available.Add(RuleDataSource.PageSpeed);
            vitals = await db.WebVitals.Where(v => v.SiteId == siteId && v.Date == vitalsDate)
                .Select(v => new VitalsSample(v.Url, v.PageTypeId, v.Strategy, v.LcpMs, v.InpMs, v.Cls, v.TtfbMs))
                .ToListAsync(ct);
        }

        // Render kapalıysa ya da hiçbir örnek ölçülemediyse kaynak yoktur; render bulguları korunur, çözülmez.
        var renders = await db.RenderSamples.AsNoTracking()
            .Where(r => r.CrawlId == crawlId && r.Status != RenderStatus.Failed)
            .ToListAsync(ct);
        if (renders.Count > 0) available.Add(RuleDataSource.Render);

        return new ExternalData(available, searchByUrl, indexByUrl, vitals, renders);
    }

    private static string? Key(string url) => Uri.TryCreate(url, UriKind.Absolute, out var uri) ? UrlKey.Normalize(uri) : null;
}
