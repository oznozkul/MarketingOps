using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Commands.EvaluateCrawl;

public sealed class EvaluateCrawlCommandHandler(IAppDbContext db, RuleEvaluator evaluator, TimeProvider clock) : IRequestHandler<EvaluateCrawlCommand>
{
    public async Task Handle(EvaluateCrawlCommand request, CancellationToken cancellationToken)
    {
        var crawl = await db.Crawls.FirstOrDefaultAsync(c => c.Id == request.CrawlId, cancellationToken);
        // Başarısız/iptal taramalar ve zaten değerlendirilmiş taramalar atlanır (iş tekrar çalışsa da güvenli).
        if (crawl is null || !crawl.CanBeEvaluated) return;
        // Sıra dışı çalışan eski bir iş, daha yeni taramanın sonucunu geri almasın.
        if (await db.Crawls.AnyAsync(c => c.SiteId == crawl.SiteId && c.Number > crawl.Number && c.EvaluatedAt != null, cancellationToken)) return;

        var startedAt = clock.GetUtcNow();
        var site = await db.Sites.AsNoTracking().Include(s => s.PageTypes).FirstAsync(s => s.Id == crawl.SiteId, cancellationToken);
        var settings = await db.LoadAsync(site.Id, cancellationToken);

        var enabled = RuleCatalog.All.Where(settings.IsEnabled).ToList();
        var violations = await evaluator.EvaluateAsync(crawl.Id, crawl.AssetChecks, site,
            enabled.Select(r => (r, settings.ThresholdsOf(r))).ToList(), cancellationToken);

        var observation = new CrawlObservation(crawl.Id, crawl.Number, crawl.IsPartial, crawl.FinishedAt!.Value);
        var findings = await db.Findings.Where(f => f.SiteId == site.Id).ToListAsync(cancellationToken);
        var byFingerprint = findings.ToDictionary(f => f.Fingerprint);
        var seen = new HashSet<string>();
        int opened = 0, reopened = 0, resolved = 0;

        foreach (var group in violations.GroupBy(v => (v.RuleCode, v.PageTypeId)))
        {
            var rule = RuleCatalog.Find(group.Key.RuleCode)!;
            var fingerprint = Finding.ComputeFingerprint(site.Id, rule.Code, group.Key.PageTypeId);
            seen.Add(fingerprint);

            var count = group.Count();
            var severity = SiteRuleSetting.EffectiveSeverity(rule, settings.GetValueOrDefault(rule.Code), group.Key.PageTypeId);
            var occurrences = group.Take(Finding.MaxOccurrencesKept).Select(v => new FindingOccurrence(v.Url, v.CurrentValue, v.Detail, crawl.Id)).ToList();

            if (byFingerprint.TryGetValue(fingerprint, out var finding))
            {
                var wasResolved = finding.Status == FindingStatus.Resolved;
                finding.Observe(rule.Title(count), severity, count, occurrences, observation);
                if (wasResolved && finding.Status == FindingStatus.Reopened) reopened++;
            }
            else
            {
                db.Findings.Add(Finding.Open(site.Id, rule.Code, group.Key.PageTypeId, rule.Category, severity, rule.Title(count), count, occurrences, observation));
                opened++;
            }
        }

        // Bu taramada çalışan bir kuralın görülmeyen bulguları. Kapalı kuralların bulgularına dokunulmaz.
        var evaluatedCodes = enabled.Select(r => r.Code).ToHashSet();
        foreach (var finding in findings.Where(f => !seen.Contains(f.Fingerprint) && evaluatedCodes.Contains(f.RuleCode)))
        {
            var wasActive = finding.IsActive;
            finding.MarkNotObserved(observation);
            if (wasActive && finding.Status == FindingStatus.Resolved) resolved++;
        }

        crawl.RecordEvaluation(startedAt, clock.GetUtcNow(), opened, resolved, reopened);
        await db.SaveChangesAsync(cancellationToken);
    }
}
