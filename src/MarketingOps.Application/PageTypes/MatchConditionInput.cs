using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.PageTypes;

public sealed record MatchConditionInput(MatchConditionType Type, string Value)
{
    public MatchCondition ToDomain() => new(Type, Value);
}
