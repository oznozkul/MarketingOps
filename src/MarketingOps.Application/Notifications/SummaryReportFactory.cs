using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Scores;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Notifications;
using MarketingOps.Domain.Scores;
using MarketingOps.Domain.WorkTasks;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications;

public sealed class SummaryReportFactory(IAppDbContext db, ScoreReader scores, TimeProvider clock)
{
    public async Task<SummaryReport> BuildAsync(Guid siteId, ReportPeriod period, CancellationToken cancellationToken)
    {
        var site = await db.Sites.AsNoTracking().Where(s => s.Id == siteId).Select(s => s.Name).FirstAsync(cancellationToken);
        var (from, to, label) = Window(period, clock.GetUtcNow());
        var facts = await scores.LoadAsync(siteId, cancellationToken);
        var snapshots = await db.ScoreSnapshots.AsNoTracking()
            .Where(s => s.SiteId == siteId && s.Date < from)
            .ToListAsync(cancellationToken);
        var previous = snapshots.GroupBy(s => s.Category).ToDictionary(g => g.Key, g => g.MaxBy(s => s.Date)!.Score);
        var lines = Enum.GetValues<ScoreCategory>().Select(category =>
        {
            var score = ScoreCalculator.Score(ScoreReader.Inputs(facts, category));
            int? delta = previous.TryGetValue(category, out var before) ? score - before : null;
            return new SummaryScoreLine(NotificationText.CategoryName(category.ToString()), score, delta);
        }).ToList();

        var issues = facts.Findings
            .OrderBy(f => f.Severity)
            .ThenByDescending(f => f.AffectedCount)
            .Take(5)
            .Select(f => new SummaryIssueLine(f.Title, SeverityName(f.Severity), f.AffectedCount))
            .ToList();

        var fromAt = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var toAt = new DateTimeOffset(to.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var tasks = await db.WorkTasks.AsNoTracking()
            .Where(t => t.SiteId == siteId && t.Status == WorkTaskStatus.Closed && t.ClosedAt >= fromAt && t.ClosedAt < toAt)
            .OrderByDescending(t => t.ClosedAt)
            .Select(t => new SummaryTaskLine(t.Key, t.Title))
            .Take(10)
            .ToListAsync(cancellationToken);

        var order = await db.OrderComparisons.AsNoTracking()
            .Where(o => o.SiteId == siteId && o.Date >= from && o.Date <= to)
            .OrderByDescending(o => o.Date)
            .Select(o => new { o.Date, o.OrderCount, o.Ga4PurchaseCount, o.GapPercent })
            .FirstOrDefaultAsync(cancellationToken);
        var orderLine = order is null
            ? "Bu dönemde sipariş karşılaştırması yok."
            : $"{order.Date:d MMM}: sipariş {order.OrderCount?.ToString() ?? "—"}, purchase {order.Ga4PurchaseCount?.ToString() ?? "—"}, fark {(order.GapPercent is null ? "—" : "%" + order.GapPercent.Value.ToString("0.#"))}.";

        return new SummaryReport(site, label, lines, issues, tasks, orderLine);
    }

    private static string SeverityName(Severity severity) => severity switch
    {
        Severity.Critical => "Kritik",
        Severity.High => "Yüksek",
        Severity.Medium => "Orta",
        _ => "Düşük"
    };

    public static (DateOnly From, DateOnly To, string Label) Window(ReportPeriod period, DateTimeOffset utcNow)
    {
        var today = IstanbulClock.Today(utcNow);
        return period switch
        {
            ReportPeriod.Weekly => (today.AddDays(-7), today.AddDays(-1), "Haftalık özet"),
            ReportPeriod.Monthly => (new DateOnly(today.Year, today.Month, 1).AddMonths(-1), new DateOnly(today.Year, today.Month, 1).AddDays(-1), "Aylık özet"),
            _ => (today.AddDays(-1), today.AddDays(-1), "Günlük özet")
        };
    }
}
