using MarketingOps.Domain.Crawls;

namespace MarketingOps.Domain.Rules;

/// <summary>Bir site kuralının tek taramalık çalışması.</summary>
public interface ISiteRuleRun
{
    void Observe(PageSnapshot page);

    IEnumerable<RuleViolation> Complete();
}
