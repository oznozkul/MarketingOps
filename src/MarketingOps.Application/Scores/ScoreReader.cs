using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Scores;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Scores;

/// <summary>Açık bulgular ve son tamamlanan taramanın sayfa tipi sayfa sayıları. Skor hesabının tek girdisi.</summary>
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
        if (crawlId is Guid id)
        {
            var counts = await db.PageSnapshots.AsNoTracking()
                .Where(p => p.CrawlId == id && p.PageTypeId != null && p.StatusCode >= 200 && p.StatusCode < 300
                    && p.ContentType != null && EF.Functions.ILike(p.ContentType, "%html%"))
                .GroupBy(p => p.PageTypeId!.Value)
                .Select(g => new { PageTypeId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            pages = counts.ToDictionary(x => x.PageTypeId, x => x.Count);
        }

        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == siteId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
            .Select(f => new ScoreFact(f.Id, f.Title, f.Severity, f.Category, f.PageTypeId, f.AffectedCount))
            .ToListAsync(cancellationToken);
        return new ScoreFacts(pages, findings);
    }

    public static ScoreInput ToInput(ScoreFact fact, IReadOnlyDictionary<Guid, int> pages) =>
        new(fact.Id, fact.Title, fact.Severity, fact.PageTypeId, fact.AffectedCount,
            fact.PageTypeId is Guid pageType && pages.TryGetValue(pageType, out var count) ? count : 0);

    public static IReadOnlyList<ScoreInput> Inputs(ScoreFacts facts, ScoreCategory category) =>
        facts.Findings.Where(f => ScoreCategoryMap.Includes(category, f.Category)).Select(f => ToInput(f, facts.Pages)).ToList();
}
