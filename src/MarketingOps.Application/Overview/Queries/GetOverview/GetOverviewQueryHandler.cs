using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules;
using MarketingOps.Application.Scores;
using MarketingOps.Application.Sites;
using MarketingOps.Application.Tracking.Queries.GetOrderComparison;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Domain.Scores;
using MarketingOps.Domain.WorkTasks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed class GetOverviewQueryHandler(IAppDbContext db, ScoreReader scores, TimeProvider clock) : IRequestHandler<GetOverviewQuery, OverviewResponse>
{
    public async Task<OverviewResponse> Handle(GetOverviewQuery request, CancellationToken cancellationToken)
    {
        var hasCrawl = await db.Crawls.AnyAsync(c => c.SiteId == request.SiteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted), cancellationToken);
        var ga4 = await db.ConnectorAccounts.AnyAsync(c => c.SiteId == request.SiteId && c.Type == ConnectorType.Ga4 && c.Status == ConnectorStatus.Connected, cancellationToken);
        if (!hasCrawl)
            return new OverviewResponse(false, ga4, null, 0, [], new OverviewTaskCounts(0, 0, 0, 0, 0, 0, 0), [], await Orders(request, cancellationToken), [], [], []);

        var facts = await scores.LoadAsync(request.SiteId, cancellationToken);
        var site = await SiteLoader.LoadAsync(db, request.SiteId, cancellationToken);
        var names = site.PageTypes.ToDictionary(p => p.Id, p => p.Name);
        var active = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
            .Select(f => new OverviewFindingRow(f.Id, f.Title, f.RuleCode, f.Severity, f.Status, f.PageTypeId, f.AffectedCount))
            .ToListAsync(cancellationToken);

        var critical = active.Where(f => f.Severity == Severity.Critical).OrderByDescending(f => f.AffectedCount).ToList();
        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var snapshots = await db.ScoreSnapshots.AsNoTracking().Where(s => s.SiteId == request.SiteId && s.Date >= today.AddDays(-30)).ToListAsync(cancellationToken);
        var previousDay = snapshots.Where(s => s.Date < today).GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.MaxBy(s => s.Date)!.Score);
        var scoreRows = Enum.GetValues<ScoreCategory>().Select(category =>
            new OverviewScoreResponse(category, ScoreCalculator.Score(ScoreReader.Inputs(facts, category)), previousDay.TryGetValue(category, out var earlier) ? earlier : null)).ToList();

        return new OverviewResponse(
            true,
            ga4,
            critical.Count == 0 ? null : Issue(critical[0], names),
            Math.Max(0, critical.Count - 1),
            scoreRows,
            await Tasks(request.SiteId, cancellationToken),
            active.OrderBy(f => f.Severity).ThenByDescending(f => f.AffectedCount).Take(6).Select(f => Issue(f, names)).ToList(),
            await Orders(request, cancellationToken),
            Trend(today, snapshots),
            await Events(request.SiteId, today, cancellationToken),
            PageTypes(site, facts, active));
    }

    private async Task<OrderComparisonResponse> Orders(GetOverviewQuery request, CancellationToken ct)
    {
        var days = request.Days == 7 ? 7 : 30;
        var from = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-days);
        var rows = await db.OrderComparisons.AsNoTracking()
            .Where(o => o.SiteId == request.SiteId && o.Date >= from)
            .OrderBy(o => o.Date)
            .Select(o => new OrderComparisonRowResponse(o.Date, o.OrderCount, o.OrderRevenue, o.Ga4PurchaseCount, o.Ga4PurchaseValue, o.GapPercent))
            .ToListAsync(ct);
        var settings = await SiteRuleSettings.LoadAsync(db, request.SiteId, ct);
        var rule = Domain.Rules.RuleCatalog.All.OfType<PurchaseGapRule>().Single();
        var values = settings.ThresholdsOf(rule);
        var threshold = values.TryGetValue(PurchaseGapRule.MaxGap.Key, out var configured) ? configured : PurchaseGapRule.MaxGap.Default;
        return new OrderComparisonResponse(threshold, rows.Any(r => r.OrderCount is not null), rows.Any(r => r.PurchaseCount is not null), rows);
    }

    private async Task<OverviewTaskCounts> Tasks(Guid siteId, CancellationToken ct)
    {
        var severities = await db.WorkTasks.AsNoTracking()
            .Where(t => t.SiteId == siteId && t.Status != WorkTaskStatus.Closed)
            .Join(db.Findings.AsNoTracking(), t => t.FindingId, f => f.Id, (_, f) => f.Severity)
            .ToListAsync(ct);
        var today = IstanbulClock.Today(clock.GetUtcNow());
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        var thisWeek = At(monday);
        var nextWeek = At(monday.AddDays(7));
        var lastWeek = At(monday.AddDays(-7));
        var closed = await db.WorkTasks.AsNoTracking()
            .Where(t => t.SiteId == siteId && t.Status == WorkTaskStatus.Closed && t.ClosedAt >= lastWeek && t.ClosedAt < nextWeek)
            .Select(t => t.ClosedAt)
            .ToListAsync(ct);
        return new OverviewTaskCounts(
            severities.Count,
            severities.Count(s => s == Severity.Critical),
            severities.Count(s => s == Severity.High),
            severities.Count(s => s == Severity.Medium),
            severities.Count(s => s == Severity.Low),
            closed.Count(t => t >= thisWeek),
            closed.Count(t => t < thisWeek));
    }

    private async Task<IReadOnlyList<OverviewTrendEvent>> Events(Guid siteId, DateOnly today, CancellationToken ct)
    {
        var from = new DateTimeOffset(today.AddDays(-29).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var rows = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == siteId && (f.FirstSeenAt >= from || f.LastSeenAt >= from))
            .Select(f => new { f.Title, f.History })
            .ToListAsync(ct);
        return rows.SelectMany(f => f.History.Where(h => h.At >= from && h.Type is FindingEventType.Opened or FindingEventType.Reopened)
                .Select(h => new { Day = DateOnly.FromDateTime(h.At.UtcDateTime), f.Title, h.Type }))
            .GroupBy(x => x.Day)
            .Select(g => new OverviewTrendEvent(g.Key, g.Count() == 1 ? g.First().Title : $"{g.Count()} bulgu açıldı veya tekrar açıldı"))
            .ToList();
    }

    private static List<OverviewTrendPoint> Trend(DateOnly today, List<ScoreSnapshot> snapshots)
    {
        var byDay = snapshots.GroupBy(s => s.Date).ToDictionary(g => g.Key, g => g.ToDictionary(s => s.Category, s => s.Score));
        return Enumerable.Range(0, 30).Select(offset =>
        {
            var date = today.AddDays(-29 + offset);
            byDay.TryGetValue(date, out var scores);
            return new OverviewTrendPoint(date, Score(scores, ScoreCategory.Tracking), Score(scores, ScoreCategory.Seo), Score(scores, ScoreCategory.Performance),
                Score(scores, ScoreCategory.Accessibility));
        }).ToList();
    }

    private static int? Score(Dictionary<ScoreCategory, int>? scores, ScoreCategory category) =>
        scores is not null && scores.TryGetValue(category, out var score) ? score : null;

    private static List<OverviewPageTypeRow> PageTypes(Domain.Sites.Site site, ScoreFacts facts, List<OverviewFindingRow> active) =>
        site.PageTypes.OrderBy(p => p.Name).Select(pageType =>
        {
            var seo = facts.Findings.Where(f => f.PageTypeId == pageType.Id && ScoreCategoryMap.Includes(ScoreCategory.Seo, f.Category))
                .Select(f => ScoreReader.ToPageTypeInput(f, facts));
            return new OverviewPageTypeRow(pageType.Id, pageType.Name, facts.Pages.GetValueOrDefault(pageType.Id), active.Count(f => f.PageTypeId == pageType.Id), ScoreCalculator.Score(seo));
        }).ToList();

    private static OverviewIssueResponse Issue(OverviewFindingRow finding, IReadOnlyDictionary<Guid, string> names) =>
        new(finding.Id, finding.Title, finding.RuleCode, finding.Severity, finding.Status, finding.AffectedCount,
            finding.PageTypeId is Guid id && names.TryGetValue(id, out var name) ? name : null);

    private static DateTimeOffset At(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue);
        // Npgsql yalnızca UTC offset'li DateTimeOffset yazar; İstanbul gece yarısını aynı anın UTC karşılığına çeviririz.
        return new DateTimeOffset(local, IstanbulClock.Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
