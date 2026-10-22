using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record MatchConditionResponse(MatchConditionType Type, string Value);
