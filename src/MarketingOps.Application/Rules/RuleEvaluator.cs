using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Sites;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules;

/// <summary>
/// Bir taramanın sayfalarını tek geçişte, akış halinde okuyup verilen kuralları çalıştırır. Sayfa verisi büyük olabileceği
/// için tüm snapshot'lar belleğe alınmaz; site kuralları yalnızca ihtiyaç duydukları özeti biriktirir.
/// </summary>
public sealed class RuleEvaluator(IAppDbContext db)
{
    public async Task<IReadOnlyList<RuleViolation>> EvaluateAsync(
        Guid crawlId, IReadOnlyList<AssetCheck> assets, Site site, ExternalData external,
        IReadOnlyList<(IRule Rule, IReadOnlyDictionary<string, decimal> Thresholds)> rules, CancellationToken ct) =>
        (await EvaluateEachAsync(crawlId, assets, site, external, rules, ct)).SelectMany(v => v).ToList();

    /// <summary>Her kural girdisinin ihlalleri ayrı listede; aynı kural farklı eşiklerle tek geçişte karşılaştırılabilir.</summary>
    public async Task<IReadOnlyList<IReadOnlyList<RuleViolation>>> EvaluateEachAsync(
        Guid crawlId, IReadOnlyList<AssetCheck> assets, Site site, ExternalData external,
        IReadOnlyList<(IRule Rule, IReadOnlyDictionary<string, decimal> Thresholds)> rules, CancellationToken ct)
    {
        var failedUrls = (await db.PageSnapshots.AsNoTracking()
                .Where(p => p.CrawlId == crawlId && (p.StatusCode == null || p.StatusCode >= 400))
                .Select(p => p.NormalizedUrl)
                .ToListAsync(ct))
            .ToHashSet();

        var pageRules = new List<(IPageRule Rule, RuleContext Context, List<RuleViolation> Output)>();
        var siteRuns = new List<(ISiteRuleRun Run, List<RuleViolation> Output)>();
        var outputs = new List<List<RuleViolation>>();
        foreach (var (rule, thresholds) in rules)
        {
            var context = new RuleContext(site, thresholds, failedUrls, assets, external);
            var output = new List<RuleViolation>();
            outputs.Add(output);
            if (rule is IPageRule pageRule) pageRules.Add((pageRule, context, output));
            else if (rule is ISiteRule siteRule) siteRuns.Add((siteRule.Start(context), output));
        }

        await foreach (var page in db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == crawlId).OrderBy(p => p.Id).AsAsyncEnumerable().WithCancellation(ct))
        {
            foreach (var (rule, context, output) in pageRules) output.AddRange(rule.Evaluate(page, context));
            foreach (var (run, _) in siteRuns) run.Observe(page);
        }
        foreach (var (run, output) in siteRuns) output.AddRange(run.Complete());
        return outputs;
    }
}
