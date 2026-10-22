using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Metrics;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Metrics.Queries.GetWebVitals;

public sealed class GetWebVitalsQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetWebVitalsQuery, WebVitalsResponse>
{
    private const int TrendDays = 28;

    public async Task<WebVitalsResponse> Handle(GetWebVitalsQuery request, CancellationToken cancellationToken)
    {
        var since = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-(TrendDays - 1));
        var rows = await db.WebVitals.AsNoTracking()
            .Where(v => v.SiteId == request.SiteId && v.Strategy == request.Strategy && v.Date >= since)
            .Select(v => new { v.Date, v.PageTypeId, v.Source, v.LcpMs, v.InpMs, v.Cls, v.TtfbMs })
            .ToListAsync(cancellationToken);
        if (rows.Count == 0) return new WebVitalsResponse(null, []);

        var names = await db.Sites.Where(s => s.Id == request.SiteId).SelectMany(s => s.PageTypes)
            .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);
        var latest = rows.Max(r => r.Date);

        // Örnek sayısı küçük; ortanca bellekte hesaplanır.
        var pageTypes = rows.GroupBy(r => r.PageTypeId).Select(g =>
        {
            var today = g.Where(r => r.Date == g.Max(x => x.Date)).ToList();
            var trend = g.GroupBy(r => r.Date).OrderBy(d => d.Key)
                .Select(d => new VitalsTrendPoint(d.Key, Median(d.Select(r => r.LcpMs)), Median(d.Select(r => r.InpMs)), Median(d.Select(r => r.Cls)), Median(d.Select(r => r.TtfbMs))))
                .ToList();
            return new PageTypeVitalsResponse(g.Key, g.Key is { } id ? names.GetValueOrDefault(id, "Silinmiş sayfa tipi") : "Eşleşmeyen sayfalar",
                today.Count, today.Min(r => r.Source), Median(today.Select(r => r.LcpMs)), Median(today.Select(r => r.InpMs)),
                Median(today.Select(r => r.Cls)), Median(today.Select(r => r.TtfbMs)), trend);
        }).OrderBy(p => p.Name, StringComparer.Create(System.Globalization.CultureInfo.GetCultureInfo("tr-TR"), false)).ToList();

        return new WebVitalsResponse(latest, pageTypes);
    }

    private static int? Median(IEnumerable<int?> values) => Median(values.Select(v => (double?)v)) is { } m ? (int)Math.Round(m) : null;

    private static double? Median(IEnumerable<double?> values)
    {
        var list = values.OfType<double>().OrderBy(v => v).ToList();
        if (list.Count == 0) return null;
        var mid = list.Count / 2;
        return list.Count % 2 == 1 ? list[mid] : (list[mid - 1] + list[mid]) / 2;
    }
}
