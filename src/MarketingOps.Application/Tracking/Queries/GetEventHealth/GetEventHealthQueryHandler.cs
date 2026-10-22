using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Application.Rules;
using MarketingOps.Domain.Rules.Definitions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tracking.Queries.GetEventHealth;

public sealed class GetEventHealthQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetEventHealthQuery, EventHealthResponse>
{
    public async Task<EventHealthResponse> Handle(GetEventHealthQuery request, CancellationToken cancellationToken)
    {
        var yesterday = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-1);
        var from = yesterday.AddDays(-29);
        var names = GoogleConnectors.Ga4Events;
        var rows = await db.Ga4Events.AsNoTracking()
            .Where(e => e.SiteId == request.SiteId && e.Date >= from && e.Date <= yesterday && names.Contains(e.EventName))
            .ToListAsync(cancellationToken);
        var settings = await SiteRuleSettings.LoadAsync(db, request.SiteId, cancellationToken);
        var rule = RuleCatalogFind();
        var values = settings.ThresholdsOf(rule);
        var threshold = values.TryGetValue(EventAnomalyRule.MaxDrop.Key, out var configured) ? configured : EventAnomalyRule.MaxDrop.Default;

        var events = names.Select(name =>
        {
            var mine = rows.Where(r => r.EventName == name).ToList();
            var series = Enumerable.Range(0, 30).Select(offset =>
            {
                var date = from.AddDays(offset);
                return new EventPointResponse(date, mine.FirstOrDefault(r => r.Date == date)?.EventCount ?? 0);
            }).ToList();
            var prior = series.Where(p => p.Date < yesterday && p.Date >= yesterday.AddDays(-7)).Select(p => p.Count).ToList();
            var average = prior.Count == 0 ? 0 : prior.Average(v => (decimal)v);
            var count = mine.FirstOrDefault(r => r.Date == yesterday)?.EventCount ?? 0;
            decimal? change = average == 0 ? null : (count - average) / average * 100m;
            var anomaly = average > 0 && (count == 0 || count < average * (1m - threshold / 100m));
            return new EventHealthRowResponse(name, count, average, change, anomaly, series);
        }).ToList();

        return new EventHealthResponse(rows.Count > 0, threshold, events);
    }

    private static EventAnomalyRule RuleCatalogFind() => Domain.Rules.RuleCatalog.All.OfType<EventAnomalyRule>().Single();
}
