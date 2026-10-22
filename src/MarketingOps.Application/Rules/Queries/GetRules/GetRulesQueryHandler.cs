using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules.Queries.GetRules;

public sealed class GetRulesQueryHandler(IAppDbContext db) : IRequestHandler<GetRulesQuery, IReadOnlyList<RuleListItemResponse>>
{
    public async Task<IReadOnlyList<RuleListItemResponse>> Handle(GetRulesQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");

        var settings = await db.LoadAsync(request.SiteId, cancellationToken);
        var counts = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId)
            .GroupBy(f => f.RuleCode)
            .Select(g => new
            {
                Code = g.Key,
                Active = g.Count(f => f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened),
                Muted = g.Count(f => f.Status == FindingStatus.FalsePositive)
            })
            .ToDictionaryAsync(c => c.Code, cancellationToken);

        return RuleCatalog.All.Select(rule =>
        {
            var count = counts.GetValueOrDefault(rule.Code);
            return RuleListItemResponse.From(rule, settings.GetValueOrDefault(rule.Code), count?.Active ?? 0, count?.Muted ?? 0);
        }).ToList();
    }
}
