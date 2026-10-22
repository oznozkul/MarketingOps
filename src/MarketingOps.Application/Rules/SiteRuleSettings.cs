using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Rules;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Rules;

public static class SiteRuleSettings
{
    /// <summary>Sitenin kayıtlı kural ayarları, kural koduna göre. Kaydı olmayan kural varsayılanlarıyla çalışır.</summary>
    public static Task<Dictionary<string, SiteRuleSetting>> LoadAsync(this IAppDbContext db, Guid siteId, CancellationToken ct) =>
        db.SiteRuleSettings.AsNoTracking().Where(s => s.SiteId == siteId).ToDictionaryAsync(s => s.RuleCode, ct);

    public static bool IsEnabled(this IReadOnlyDictionary<string, SiteRuleSetting> settings, IRule rule) =>
        settings.GetValueOrDefault(rule.Code)?.IsEnabled ?? true;

    public static IReadOnlyDictionary<string, decimal> ThresholdsOf(this IReadOnlyDictionary<string, SiteRuleSetting> settings, IRule rule) =>
        settings.GetValueOrDefault(rule.Code)?.Thresholds ?? new Dictionary<string, decimal>();
}
