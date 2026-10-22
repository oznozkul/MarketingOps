using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules;
using MarketingOps.Domain.Rules.Definitions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tracking.Queries.GetOrderComparison;

public sealed class GetOrderComparisonQueryHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<GetOrderComparisonQuery, OrderComparisonResponse>
{
    public async Task<OrderComparisonResponse> Handle(GetOrderComparisonQuery request, CancellationToken cancellationToken)
    {
        var from = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime).AddDays(-30);
        var rows = await db.OrderComparisons.AsNoTracking()
            .Where(o => o.SiteId == request.SiteId && o.Date >= from)
            .OrderBy(o => o.Date)
            .Select(o => new OrderComparisonRowResponse(o.Date, o.OrderCount, o.OrderRevenue, o.Ga4PurchaseCount, o.Ga4PurchaseValue, o.GapPercent))
            .ToListAsync(cancellationToken);
        var settings = await SiteRuleSettings.LoadAsync(db, request.SiteId, cancellationToken);
        var rule = Domain.Rules.RuleCatalog.All.OfType<PurchaseGapRule>().Single();
        var values = settings.ThresholdsOf(rule);
        var threshold = values.TryGetValue(PurchaseGapRule.MaxGap.Key, out var configured) ? configured : PurchaseGapRule.MaxGap.Default;
        return new OrderComparisonResponse(threshold, rows.Any(r => r.OrderCount is not null), rows.Any(r => r.PurchaseCount is not null), rows);
    }
}
