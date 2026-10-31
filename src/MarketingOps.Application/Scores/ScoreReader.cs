using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Scores;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Scores;

/// <summary>Açık bulgular ve son tamamlanan taramanın sayfa sayıları (tip başına ve toplam). Skor hesabının tek girdisi.</summary>
public sealed class ScoreReader(IAppDbContext db)
{
    public async Task<ScoreFacts> LoadAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var crawlId = await db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == siteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .OrderByDescending(c => c.Number)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var pages = new Dictionary<Guid, int>();
        var sitePages = 0;
        if (crawlId is Guid id)
        {
            var counts = await db.PageSnapshots.AsNoTracking()
                .Where(p => p.CrawlId == id && p.StatusCode >= 200 && p.StatusCode < 300
                    && p.ContentType != null && p.ContentType.Contains("html"))
                .GroupBy(p => p.PageTypeId)
                .Select(g => new { PageTypeId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            pages = counts.Where(x => x.PageTypeId != null).ToDictionary(x => x.PageTypeId!.Value, x => x.Count);
            sitePages = counts.Sum(x => x.Count);
        }

        var samples = new Dictionary<(RuleDataSource, Guid), int>();
        if (crawlId is Guid rendered)
        {
            var renders = await db.RenderSamples.AsNoTracking()
                .Where(r => r.CrawlId == rendered && r.PageTypeId != null && r.Status != RenderStatus.Failed)
                .GroupBy(r => r.PageTypeId!.Value)
                .Select(g => new { PageTypeId = g.Key, Count = g.Select(r => r.Url).Distinct().Count() })
                .ToListAsync(cancellationToken);
            foreach (var r in renders) samples[(RuleDataSource.Render, r.PageTypeId)] = r.Count;
        }
        var vitalsDate = await db.WebVitals.Where(v => v.SiteId == siteId).MaxAsync(v => (DateOnly?)v.Date, cancellationToken);
        if (vitalsDate is DateOnly date)
        {
            var vitals = await db.WebVitals.AsNoTracking()
                .Where(v => v.SiteId == siteId && v.Date == date && v.PageTypeId != null)
                .GroupBy(v => v.PageTypeId!.Value)
                .Select(g => new { PageTypeId = g.Key, Count = g.Select(v => v.Url).Distinct().Count() })
                .ToListAsync(cancellationToken);
            foreach (var v in vitals) samples[(RuleDataSource.PageSpeed, v.PageTypeId)] = v.Count;
        }

        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == siteId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
            .Select(f => new ScoreFact(f.Id, f.Title, f.Severity, f.Category, f.RuleCode, f.PageTypeId, f.AffectedCount))
            .ToListAsync(cancellationToken);
        return new ScoreFacts(pages, sitePages, samples, findings);
    }

    /// <summary>Sayfa tipi skoru için: oran o tipin sayfa sayısına, örneklenmiş kaynakta ölçülen örnek sayısına göre.</summary>
    public static ScoreInput ToPageTypeInput(ScoreFact fact, ScoreFacts facts)
    {
        if (SampleCount(fact, facts) is int sampled) return ToInput(fact, fact.AffectedCount, sampled);
        return ToInput(fact, fact.AffectedCount, fact.PageTypeId is Guid pageType && facts.Pages.TryGetValue(pageType, out var count) ? count : 0);
    }

    /// <summary>
    /// Site skoru için oran sitenin bütün HTML sayfalarına göredir (sayfa tipine uymayan sayfa dahil).
    /// Örnek ölçen kaynaklarda (render, PageSpeed) sorun şablonundur: tipin ölçülen örneklerindeki oran tipin bütün
    /// sayfalarına yansıtılır (3 örneğin 2'si sorunluysa tipin üçte ikisi). Örnek sayısı bilinmeyen ya da sayfaya dayanmayan
    /// bulgu (tracking denetimi, purchase farkı) tam ağırlık düşürür; kural sınırı her durumda geçerlidir.
    /// </summary>
    public static IReadOnlyList<ScoreInput> Inputs(ScoreFacts facts, ScoreCategory category) =>
        facts.Findings
            .Where(f => ScoreCategoryMap.Includes(category, f.Category))
            .Select(f => SiteInput(f, facts))
            .ToList();

    private static ScoreInput SiteInput(ScoreFact fact, ScoreFacts facts)
    {
        var source = RuleCatalog.Find(fact.RuleCode)?.DataSource;
        if (source is RuleDataSource.Crawl or RuleDataSource.FullCrawl or RuleDataSource.SearchConsole or RuleDataSource.UrlInspection)
            return ToInput(fact, fact.AffectedCount, facts.SitePages);
        if (SampleCount(fact, facts) is int sampled && fact.PageTypeId is Guid pageType && facts.Pages.TryGetValue(pageType, out var typePages))
        {
            var represented = (int)Math.Round(Math.Min(1.0, (double)fact.AffectedCount / sampled) * typePages, MidpointRounding.AwayFromZero);
            return ToInput(fact, represented, facts.SitePages);
        }
        return ToInput(fact, fact.AffectedCount, 0);
    }

    private static int? SampleCount(ScoreFact fact, ScoreFacts facts) =>
        RuleCatalog.Find(fact.RuleCode)?.DataSource is { } source && fact.PageTypeId is Guid pageType
        && facts.Samples.TryGetValue((source, pageType), out var count) && count > 0 ? count : null;

    private static ScoreInput ToInput(ScoreFact fact, int affected, int pageCount) =>
        new(fact.Id, fact.Title, fact.Severity, fact.RuleCode, fact.PageTypeId, affected, pageCount);
}
