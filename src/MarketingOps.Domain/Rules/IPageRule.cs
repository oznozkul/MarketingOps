using MarketingOps.Domain.Crawls;

namespace MarketingOps.Domain.Rules;

public interface IPageRule : IRule
{
    IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context);
}
