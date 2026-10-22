using MarketingOps.Domain.Common;
using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Rules;

/// <summary>Bir kuralın site bazlı ayarı. Kayıt yoksa kuralın varsayılanları geçerlidir.</summary>
public class SiteRuleSetting : Entity
{
    private Dictionary<Guid, Severity> _pageTypeSeverities = [];
    private Dictionary<string, decimal> _thresholds = [];

    public Guid SiteId { get; private set; }
    public string RuleCode { get; private set; } = null!;
    public bool IsEnabled { get; private set; } = true;
    public Severity? SeverityOverride { get; private set; }
    public IReadOnlyDictionary<Guid, Severity> PageTypeSeverities => _pageTypeSeverities;
    public IReadOnlyDictionary<string, decimal> Thresholds => _thresholds;

    private SiteRuleSetting() { }

    public static SiteRuleSetting Create(Guid siteId, IRule rule) => new() { SiteId = siteId, RuleCode = rule.Code };

    /// <summary>Eşikler kuralın tanımına göre doğrulanır: bilinmeyen anahtar ya da aralık dışı değer reddedilir.</summary>
    public void Update(IRule rule, bool isEnabled, Severity? severityOverride, IReadOnlyDictionary<Guid, Severity> pageTypeSeverities, IReadOnlyDictionary<string, decimal> thresholds)
    {
        if (rule.Code != RuleCode) throw new DomainException("Ayar başka bir kurala ait.");
        foreach (var (key, value) in thresholds)
        {
            var definition = rule.Thresholds.FirstOrDefault(t => t.Key == key)
                ?? throw new DomainException($"'{key}' bu kural için geçerli bir eşik değil.");
            if (value < definition.Min || value > definition.Max)
                throw new DomainException($"{definition.Label} {definition.Min:0.##}–{definition.Max:0.##} {definition.Unit} arasında olmalı.");
        }

        IsEnabled = isEnabled;
        SeverityOverride = severityOverride;
        _pageTypeSeverities = new Dictionary<Guid, Severity>(pageTypeSeverities);
        _thresholds = new Dictionary<string, decimal>(thresholds);
    }

    /// <summary>Önem sırası: sayfa tipine özel ayar → site geneli ayar → kuralın varsayılanı.</summary>
    public static Severity EffectiveSeverity(IRule rule, SiteRuleSetting? setting, Guid? pageTypeId) =>
        pageTypeId is { } id && setting is not null && setting.PageTypeSeverities.TryGetValue(id, out var byType)
            ? byType
            : setting?.SeverityOverride ?? rule.DefaultSeverity;
}
