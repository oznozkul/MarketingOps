using MarketingOps.Domain.Common;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;

namespace MarketingOps.UnitTests.Domain.Rules;

public class SiteRuleSettingTests
{
    private static readonly IRule Rule = new TitleLengthRule();

    [Fact]
    public void Threshold_outside_the_rule_range_is_rejected()
    {
        var setting = SiteRuleSetting.Create(Guid.CreateVersion7(), Rule);
        Should.Throw<DomainException>(() => setting.Update(Rule, true, null, new Dictionary<Guid, Severity>(), new Dictionary<string, decimal> { ["minLength"] = 5 }));
        Should.Throw<DomainException>(() => setting.Update(Rule, true, null, new Dictionary<Guid, Severity>(), new Dictionary<string, decimal> { ["bilinmeyen"] = 40 }));
    }

    [Fact]
    public void Effective_severity_prefers_page_type_then_site_then_default()
    {
        var blog = Guid.CreateVersion7();
        var setting = SiteRuleSetting.Create(Guid.CreateVersion7(), Rule);
        SiteRuleSetting.EffectiveSeverity(Rule, null, blog).ShouldBe(Rule.DefaultSeverity);

        setting.Update(Rule, true, Severity.High, new Dictionary<Guid, Severity> { [blog] = Severity.Critical }, new Dictionary<string, decimal>());
        SiteRuleSetting.EffectiveSeverity(Rule, setting, blog).ShouldBe(Severity.Critical);
        SiteRuleSetting.EffectiveSeverity(Rule, setting, Guid.CreateVersion7()).ShouldBe(Severity.High);
        SiteRuleSetting.EffectiveSeverity(Rule, setting, null).ShouldBe(Severity.High);
    }
}
