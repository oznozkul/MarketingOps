using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
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

        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == siteId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
            .Select(f => new ScoreFact(f.Id, f.Title, f.Severity, f.Category, f.RuleCode, f.PageTypeId, f.AffectedCount))
            .ToListAsync(cancellationToken);
        return new ScoreFacts(pages, sitePages, findings);
    }

    /// <summary>Sayfa tipi skoru için: oran o tipin sayfa sayısına göre.</summary>
    public static ScoreInput ToPageTypeInput(ScoreFact fact, IReadOnlyDictionary<Guid, int> pages) =>
        ToInput(fact, fact.PageTypeId is Guid pageType && pages.TryGetValue(pageType, out var count) ? count : 0);

    /// <summary>
    /// Site skoru için: taranan sayfalara dayanan kurallarda oran sitenin bütün HTML sayfalarına göre (sayfa tipine uymayan
    /// sayfa dahil). Örnek sayfa ölçen kaynaklarda (tracking denetimi, PageSpeed) etkilenen sayı siteyi temsil etmez;
    /// bulgu tam ağırlık düşürür, kural sınırı yine geçerlidir.
    /// </summary>
    public static IReadOnlyList<ScoreInput> Inputs(ScoreFacts facts, ScoreCategory category) =>
        facts.Findings
            .Where(f => ScoreCategoryMap.Includes(category, f.Category))
            .Select(f => ToInput(f, IsPageBased(f.RuleCode) ? facts.SitePages : 0))
            .ToList();

    private static bool IsPageBased(string ruleCode) =>
        RuleCatalog.Find(ruleCode)?.DataSource is RuleDataSource.Crawl or RuleDataSource.SearchConsole or RuleDataSource.UrlInspection;

    private static ScoreInput ToInput(ScoreFact fact, int pageCount) =>
        new(fact.Id, fact.Title, fact.Severity, fact.RuleCode, fact.PageTypeId, fact.AffectedCount, pageCount);
}
