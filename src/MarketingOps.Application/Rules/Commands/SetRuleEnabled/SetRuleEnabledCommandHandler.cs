using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Rules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules.Commands.SetRuleEnabled;

public sealed class SetRuleEnabledCommandHandler(IAppDbContext db) : IRequestHandler<SetRuleEnabledCommand, Guid>
{
    public async Task<Guid> Handle(SetRuleEnabledCommand request, CancellationToken cancellationToken)
    {
        if (!await db.Sites.AnyAsync(s => s.Id == request.SiteId, cancellationToken)) throw new NotFoundException("Site bulunamadı.");
        var rule = RuleCatalog.Find(request.RuleCode)!;

        var setting = await db.SiteRuleSettings.FirstOrDefaultAsync(s => s.SiteId == request.SiteId && s.RuleCode == rule.Code, cancellationToken);
        if (setting is null)
        {
            setting = SiteRuleSetting.Create(request.SiteId, rule);
            db.SiteRuleSettings.Add(setting);
        }
        setting.Update(rule, request.IsEnabled, setting.SeverityOverride, setting.PageTypeSeverities, setting.Thresholds);
        await db.SaveChangesAsync(cancellationToken);
        return setting.Id;
    }
}
