using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules.Queries.GetRule;

public sealed class GetRuleQueryHandler(IAppDbContext db) : IRequestHandler<GetRuleQuery, RuleDetailResponse>
{
    public async Task<RuleDetailResponse> Handle(GetRuleQuery request, CancellationToken cancellationToken)
    {
        var rule = RuleCatalog.Find(request.RuleCode) ?? throw new NotFoundException("Kural bulunamadı.");
        var pageTypes = await db.Sites.AsNoTracking()
            .Where(s => s.Id == request.SiteId)
            .SelectMany(s => s.PageTypes)
            .OrderBy(p => p.Name)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(cancellationToken);
        if (pageTypes.Count == 0 && !await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken))
            throw new NotFoundException("Site bulunamadı.");

        var setting = await db.SiteRuleSettings.AsNoTracking().FirstOrDefaultAsync(s => s.SiteId == request.SiteId && s.RuleCode == rule.Code, cancellationToken);
        var findings = await db.Findings.AsNoTracking()
            .Where(f => f.SiteId == request.SiteId && f.RuleCode == rule.Code)
            .Select(f => new { f.Id, f.Title, f.Severity, f.Status, f.PageTypeId, f.AffectedCount, f.FalsePositiveReason, f.History })
            .ToListAsync(cancellationToken);
        var names = pageTypes.ToDictionary(p => p.Id, p => p.Name);
        string? NameOf(Guid? id) => id is { } value ? names.GetValueOrDefault(value) : null;

        var muted = findings.Where(f => f.Status == FindingStatus.FalsePositive).ToList();
        var mutedIds = muted.Select(f => f.PageTypeId).ToHashSet();
        var active = findings.Where(f => f.Status is FindingStatus.Open or FindingStatus.Reopened).ToList();

        return new RuleDetailResponse(
            RuleListItemResponse.From(rule, setting, active.Count, muted.Count),
            rule.Impact,
            rule.ReferenceKey,
            setting?.SeverityOverride,
            rule.Thresholds
                .Select(t => new RuleThresholdResponse(t.Key, t.Label, t.Unit, t.Default, t.Min, t.Max, setting?.Thresholds.GetValueOrDefault(t.Key, t.Default) ?? t.Default))
                .ToList(),
            pageTypes
                .Select(p => new RulePageTypeResponse(p.Id, p.Name, setting?.PageTypeSeverities.TryGetValue(p.Id, out var s) == true ? s : null, mutedIds.Contains(p.Id)))
                .ToList(),
            findings
                .Where(f => f.Status != FindingStatus.FalsePositive)
                .OrderBy(f => f.Status is FindingStatus.Resolved).ThenBy(f => f.Severity).ThenByDescending(f => f.AffectedCount)
                .Select(f => new RuleFindingResponse(f.Id, f.Title, f.Severity, f.Status, NameOf(f.PageTypeId), f.AffectedCount))
                .ToList(),
            muted
                .Select(f =>
                {
                    var marked = f.History.LastOrDefault(h => h.Type == FindingEventType.MarkedFalsePositive);
                    return new MutedPageTypeResponse(f.Id, NameOf(f.PageTypeId), f.FalsePositiveReason, marked?.Actor, marked?.At);
                })
                .ToList());
    }
}
