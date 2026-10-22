namespace MarketingOps.Domain.Rules;

/// <summary>Taramadaki sayfaları birlikte değerlendiren kural. Sayfalar akış halinde verilir; kural yalnızca ihtiyacını biriktirir.</summary>
public interface ISiteRule : IRule
{
    ISiteRuleRun Start(RuleContext context);
}
