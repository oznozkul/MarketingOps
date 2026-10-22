using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record PageTypeResponse(Guid Id, string Name, MatchMode Mode, IReadOnlyList<MatchConditionResponse> Conditions);
