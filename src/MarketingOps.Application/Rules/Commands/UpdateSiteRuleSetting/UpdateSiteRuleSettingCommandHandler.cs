using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules.Commands.UpdateSiteRuleSetting;

public sealed class UpdateSiteRuleSettingCommandHandler(IAppDbContext db) : IRequestHandler<UpdateSiteRuleSettingCommand, Guid>
{
    public async Task<Guid> Handle(UpdateSiteRuleSettingCommand request, CancellationToken cancellationToken)
    {
        var rule = RuleCatalog.Find(request.RuleCode)!;
        var pageTypeIds = await db.Sites.Where(s => s.Id == request.SiteId).SelectMany(s => s.PageTypes).Select(p => p.Id).ToListAsync(cancellationToken);
        if (pageTypeIds.Count == 0 && !await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken))
            throw new NotFoundException("Site bulunamadı.");
        if (request.PageTypeSeverities.Keys.Any(id => !pageTypeIds.Contains(id)))
            throw new NotFoundException("Sayfa tipi bulunamadı.");

        var setting = await db.SiteRuleSettings.FirstOrDefaultAsync(s => s.SiteId == request.SiteId && s.RuleCode == rule.Code, cancellationToken);
        if (setting is null)
        {
            setting = SiteRuleSetting.Create(request.SiteId, rule);
            db.SiteRuleSettings.Add(setting);
        }

        // Varsayılana eşit eşikler saklanmaz; kuralın varsayılanı değişirse site onu izlemeye devam eder.
        var thresholds = request.Thresholds
            .Where(t => rule.Thresholds.First(d => d.Key == t.Key).Default != t.Value)
            .ToDictionary(t => t.Key, t => t.Value);
        setting.Update(rule, request.IsEnabled, request.SeverityOverride, request.PageTypeSeverities, thresholds);
        await db.SaveChangesAsync(cancellationToken);
        return setting.Id;
    }
}
