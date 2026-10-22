using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;

public sealed class PreviewRuleThresholdImpactQueryHandler(IAppDbContext db, RuleEvaluator evaluator, ExternalDataLoader externalData)
    : IRequestHandler<PreviewRuleThresholdImpactQuery, ThresholdImpactResponse>
{
    public async Task<ThresholdImpactResponse> Handle(PreviewRuleThresholdImpactQuery request, CancellationToken cancellationToken)
    {
        var rule = RuleCatalog.Find(request.RuleCode)!;
        var site = await db.Sites.AsNoTracking().Include(s => s.PageTypes).FirstOrDefaultAsync(s => s.Id == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Site bulunamadı.");
        var crawl = await db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == site.Id && c.EvaluatedAt != null)
            .OrderByDescending(c => c.Number)
            .FirstOrDefaultAsync(cancellationToken);
        if (crawl is null) return new ThresholdImpactResponse(null, 0, 0, 0, 0, 0);

        var settings = await db.LoadAsync(site.Id, cancellationToken);
        var results = await evaluator.EvaluateEachAsync(crawl.Id, crawl.AssetChecks, site, await externalData.LoadAsync(site.Id, cancellationToken),
            [(rule, settings.ThresholdsOf(rule)), (rule, request.Thresholds)], cancellationToken);

        var current = Group(results[0]);
        var proposed = Group(results[1]);
        var affected = current.Keys.Union(proposed.Keys).Count(key => current.GetValueOrDefault(key) != proposed.GetValueOrDefault(key));
        return new ThresholdImpactResponse(crawl.Number, results[0].Count, results[1].Count, current.Count, proposed.Count, affected);
    }

    // Bulgu, sayfa tipi başına bir gruptur; değer gruptaki sayfa sayısı.
    private static Dictionary<Guid, int> Group(IReadOnlyList<RuleViolation> violations) =>
        violations.GroupBy(v => v.PageTypeId ?? Guid.Empty).ToDictionary(g => g.Key, g => g.Count());
}
