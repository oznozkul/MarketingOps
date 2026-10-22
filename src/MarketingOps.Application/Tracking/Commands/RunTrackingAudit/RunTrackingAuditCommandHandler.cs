using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Scores.Commands.RecordScoreSnapshots;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Application.Rules;
using MarketingOps.Application.Sites;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Domain.Tracking;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Tracking.Commands.RunTrackingAudit;

public sealed class RunTrackingAuditCommandHandler(
    IAppDbContext db,
    ITrackingBrowser browser,
    IOrderSource orders,
    IMetaGraphClient metaGraph,
    ConnectorSecretProtector protector,
    ISender sender,
    TimeProvider clock,
    ILogger<RunTrackingAuditCommandHandler> logger) : IRequestHandler<RunTrackingAuditCommand, Guid>
{
    private const int ComparisonDays = 30;
    private const string PurchaseEvent = "purchase";

    public async Task<Guid> Handle(RunTrackingAuditCommand request, CancellationToken cancellationToken)
    {
        if (await db.TagAuditRuns.AnyAsync(r => r.SiteId == request.SiteId && r.Status == AuditStatus.Running, cancellationToken))
            throw new ConflictException("Bu sitede bir denetim zaten çalışıyor.");

        var site = await SiteLoader.LoadAsync(db, request.SiteId, cancellationToken);
        var now = clock.GetUtcNow();
        var number = (await db.TagAuditRuns.Where(r => r.SiteId == site.Id).MaxAsync(r => (int?)r.Number, cancellationToken) ?? 0) + 1;
        var run = TagAuditRun.Start(site.Id, number, now);
        db.TagAuditRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var pages = await RepresentativePages(site, cancellationToken);
            var observations = pages.Count == 0
                ? []
                : await browser.ProbeAsync(pages.Select(p => p.Url).Distinct().ToList(), site.Journey.ConsentButtonSelector, cancellationToken);
            var audited = Audited(pages, observations);
            var cells = TagAuditBuilder.Build(run.Id, audited, site.ExpectedTrackingIds);
            var consentTested = site.Journey.ConsentButtonSelector is not null && observations.Any(o => o.ConsentClicked);

            var (orderError, comparisons) = await WriteComparisons(site, cancellationToken);
            run.NoteOrderSource(orderError);
            run.Complete(cells, consentTested, clock.GetUtcNow());
            await ApplyFindings(site, run, cells, audited.Count > 0, comparisons, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await sender.Send(new RecordScoreSnapshotsCommand(site.Id), cancellationToken);
        }
        catch (Exception ex) when (ex is not ConflictException)
        {
            logger.LogWarning(ex, "Tracking denetimi tamamlanamadı. Site {SiteId}, denetim {AuditId}.", site.Id, run.Id);
            run.Fail("Denetim tamamlanamadı.", clock.GetUtcNow());
            await db.SaveChangesAsync(CancellationToken.None);
            if (ex is OperationCanceledException) throw;
        }

        return run.Id;
    }

    private async Task<List<(string Label, Guid? PageTypeId, string Url)>> RepresentativePages(Domain.Sites.Site site, CancellationToken ct)
    {
        var pages = new List<(string Label, Guid? PageTypeId, string Url)> { ("Ana sayfa", null, $"https://{site.Domain}/") };
        var crawlId = await db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == site.Id && c.Status == CrawlStatus.Completed)
            .OrderByDescending(c => c.Number)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);
        if (crawlId is Guid id)
        {
            var snaps = await db.PageSnapshots.AsNoTracking()
                .Where(p => p.CrawlId == id && p.PageTypeId != null && p.StatusCode >= 200 && p.StatusCode < 300
                    && p.ContentType != null && p.ContentType.Contains("html"))
                .Select(p => new { p.PageTypeId, p.Url })
                .ToListAsync(ct);
            foreach (var type in site.PageTypes.OrderBy(t => t.Name))
            {
                var url = snaps.FirstOrDefault(s => s.PageTypeId == type.Id)?.Url;
                if (url is not null) pages.Add((Unique(pages, type.Name), type.Id, url));
            }
        }

        AddJourney(pages, "Sepet", site.Journey.CartUrl);
        AddJourney(pages, "Ödeme", site.Journey.CheckoutUrl);
        AddJourney(pages, "Teşekkürler", site.Journey.ThankYouUrl);
        return pages;
    }

    private static void AddJourney(List<(string Label, Guid? PageTypeId, string Url)> pages, string label, string? url)
    {
        if (url is not null) pages.Add((Unique(pages, label), null, url));
    }

    private static string Unique(List<(string Label, Guid? PageTypeId, string Url)> pages, string label)
    {
        var name = label;
        var n = 2;
        while (pages.Any(p => p.Label == name)) name = $"{label} {n++}";
        return name;
    }

    private static List<AuditedPage> Audited(IReadOnlyList<(string Label, Guid? PageTypeId, string Url)> pages, IReadOnlyList<PageObservation> observations)
    {
        var byUrl = observations.GroupBy(o => o.Url).ToDictionary(g => g.Key, g => g.First());
        var audited = new List<AuditedPage>();
        foreach (var page in pages)
        {
            if (!byUrl.TryGetValue(page.Url, out var observation) || !observation.Loaded) continue;
            var requests = observation.Requests
                .Select(r => TagRequestClassifier.Classify(r.Url, r.BeforeConsent))
                .OfType<CapturedTagRequest>()
                .ToList();
            audited.Add(new AuditedPage(page.Label, page.PageTypeId, page.Url, requests));
        }
        return audited;
    }

    /// <summary>Sipariş kaynağı yoksa veya sorgu düşerse satır yine yazılır; purchase boş kalır ve fark kuralı çalışmaz.</summary>
    private async Task<(string? Error, List<OrderComparisonDaily> Rows)> WriteComparisons(Domain.Sites.Site site, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var to = today.AddDays(-1);
        var from = to.AddDays(-(ComparisonDays - 1));

        string? error = null;
        IReadOnlyList<OrderDayStats> stats = [];
        if (site.OrderSource.IsConfigured)
        {
            try
            {
                stats = await orders.GetDailyOrderStatsAsync(
                    protector.Unprotect(site.OrderSource.ProtectedConnection!), site.OrderSource.Query!, from, to, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                error = "Sipariş sorgusu çalışmadı. " + ex.Message;
            }
        }

        var purchases = await db.Ga4Events.AsNoTracking()
            .Where(e => e.SiteId == site.Id && e.Date >= from && e.Date <= to && e.EventName.ToLower() == PurchaseEvent)
            .ToListAsync(ct);
        var existing = await db.OrderComparisons
            .Where(o => o.SiteId == site.Id && o.Date >= from && o.Date <= to)
            .ToListAsync(ct);
        var written = new List<OrderComparisonDaily>();

        for (var date = from; date <= to; date = date.AddDays(1))
        {
            var order = stats.FirstOrDefault(s => s.Date == date);
            var purchase = purchases.FirstOrDefault(p => p.Date == date);
            // Kaynak yoksa veya sorgu düşerse sipariş boş kalır. GA4 satırı yoksa purchase da boş kalır.
            int? count = site.OrderSource.IsConfigured && error is null ? order?.Count ?? 0 : null;
            decimal? revenue = count is null ? null : order?.Revenue ?? 0;
            long? ga4Count = purchase is null ? null : purchase.EventCount;
            decimal? ga4Value = purchase is null ? null : purchase.EventValue;

            var row = existing.FirstOrDefault(o => o.Date == date);
            if (row is null)
            {
                row = OrderComparisonDaily.Create(site.Id, date, count, revenue, ga4Count, ga4Value);
                db.OrderComparisons.Add(row);
            }
            else row.Update(count, revenue, ga4Count, ga4Value);
            written.Add(row);
        }

        return (error, written);
    }

    private async Task ApplyFindings(Domain.Sites.Site site, TagAuditRun run, IReadOnlyList<TagAuditResult> cells, bool tagsAudited, IReadOnlyList<OrderComparisonDaily> comparisons, CancellationToken ct)
    {
        var settings = await SiteRuleSettings.LoadAsync(db, site.Id, ct);
        var violations = new List<RuleViolation>();
        var ran = new HashSet<string>();
        var home = $"https://{site.Domain}/";

        if (tagsAudited)
        {
            Collect(ran, violations, settings, Rule<TagMissingRule>(), r => r.Evaluate(cells));
            Collect(ran, violations, settings, Rule<TagDuplicateRule>(), r => r.Evaluate(cells));
            Collect(ran, violations, settings, Rule<TagWrongIdRule>(), r => r.Evaluate(cells));
            Collect(ran, violations, settings, Rule<ConsentPrefireRule>(), r => r.Evaluate(cells));
        }

        if (comparisons.Any(o => o.GapPercent is not null))
        {
            var gap = Rule<PurchaseGapRule>();
            Collect(ran, violations, settings, gap, r => r.Evaluate(home, comparisons, Threshold(settings, r, PurchaseGapRule.MaxGap)));
        }

        var events = await EventDays(site.Id, ct);
        if (events.Count > 0)
        {
            var anomaly = Rule<EventAnomalyRule>();
            Collect(ran, violations, settings, anomaly, r => r.Evaluate(home, events, Threshold(settings, r, EventAnomalyRule.MaxDrop)));
        }

        await ApplyMeta(site, home, settings, ran, violations, ct);

        var observation = new CrawlObservation(run.Id, run.Number, false, run.FinishedAt!.Value);
        var findings = await db.Findings.Where(f => f.SiteId == site.Id).ToListAsync(ct);
        var byFingerprint = findings.ToDictionary(f => f.Fingerprint);
        var seen = new HashSet<string>();

        foreach (var group in violations.GroupBy(v => (v.RuleCode, v.PageTypeId)))
        {
            var rule = RuleCatalog.Find(group.Key.RuleCode)!;
            var fingerprint = Finding.ComputeFingerprint(site.Id, rule.Code, group.Key.PageTypeId);
            seen.Add(fingerprint);
            var count = group.Count();
            var severity = SiteRuleSetting.EffectiveSeverity(rule, settings.GetValueOrDefault(rule.Code), group.Key.PageTypeId);
            var occurrences = group.Take(Finding.MaxOccurrencesKept)
                .Select(v => new FindingOccurrence(v.Url, v.CurrentValue, v.Detail, run.Id))
                .ToList();
            if (byFingerprint.TryGetValue(fingerprint, out var finding))
                finding.Observe(rule.Title(count), severity, count, occurrences, observation);
            else
                db.Findings.Add(Finding.Open(site.Id, rule.Code, group.Key.PageTypeId, rule.Category, severity, rule.Title(count), count, occurrences, observation));
        }

        foreach (var finding in findings.Where(f => !seen.Contains(f.Fingerprint) && ran.Contains(f.RuleCode)))
            finding.MarkNotObserved(observation);
    }

    private async Task ApplyMeta(Domain.Sites.Site site, string home, Dictionary<string, SiteRuleSetting> settings, HashSet<string> ran, List<RuleViolation> violations, CancellationToken ct)
    {
        var pixel = site.ExpectedTrackingIds.MetaPixelId;
        var account = await db.ConnectorAccounts.FirstOrDefaultAsync(a => a.SiteId == site.Id && a.Type == ConnectorType.Meta && a.ProtectedSecret != null, ct);
        if (account?.ProtectedSecret is null || pixel is null) return;

        MetaSignals signals;
        try
        {
            signals = await metaGraph.ReadAsync(protector.Unprotect(account.ProtectedSecret), pixel, ct);
            account.RecordSync(clock.GetUtcNow());
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            account.RecordFailure(ex.Message, ex is MetaGraphAuthException, clock.GetUtcNow());
            return;
        }

        var pixelRule = Rule<MetaPixelCapiRule>();
        if (settings.IsEnabled(pixelRule) && (!signals.PixelActive || signals.CapiActive is not null))
        {
            ran.Add(pixelRule.Code);
            violations.AddRange(pixelRule.Evaluate(home, signals));
        }

        var dedup = Rule<MetaDedupRule>();
        if (settings.IsEnabled(dedup) && signals.EventIdDedup is not null)
        {
            ran.Add(dedup.Code);
            violations.AddRange(dedup.Evaluate(home, signals));
        }
    }

    private async Task<List<EventDay>> EventDays(Guid siteId, CancellationToken ct)
    {
        var yesterday = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1);
        var from = yesterday.AddDays(-7);
        var names = GoogleConnectors.Ga4Events;
        var rows = await db.Ga4Events.AsNoTracking()
            .Where(e => e.SiteId == siteId && e.Date >= from && e.Date <= yesterday && names.Contains(e.EventName))
            .ToListAsync(ct);
        if (rows.Count == 0) return [];

        return names.Select(name =>
        {
            var mine = rows.Where(r => r.EventName == name).ToList();
            var prior = Enumerable.Range(1, 7).Select(offset => mine.FirstOrDefault(r => r.Date == yesterday.AddDays(-offset))?.EventCount ?? 0).ToList();
            var average = prior.Count == 0 ? 0 : prior.Average(v => (decimal)v);
            var count = mine.FirstOrDefault(r => r.Date == yesterday)?.EventCount ?? 0;
            return new EventDay(name, count, average);
        }).ToList();
    }

    private static void Collect<TRule>(HashSet<string> ran, List<RuleViolation> violations, Dictionary<string, SiteRuleSetting> settings, TRule rule, Func<TRule, IEnumerable<RuleViolation>> evaluate)
        where TRule : IRule
    {
        if (!settings.IsEnabled(rule)) return;
        ran.Add(rule.Code);
        violations.AddRange(evaluate(rule));
    }

    private static TRule Rule<TRule>() where TRule : class, IRule => RuleCatalog.All.OfType<TRule>().Single();

    private static decimal Threshold(Dictionary<string, SiteRuleSetting> settings, IRule rule, RuleThreshold threshold)
    {
        var values = settings.ThresholdsOf(rule);
        return values.TryGetValue(threshold.Key, out var value) ? value : threshold.Default;
    }
}
