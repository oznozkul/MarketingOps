using MediatR;

namespace MarketingOps.Application.Rules.Queries.GetRule;

public sealed record GetRuleQuery(Guid SiteId, string RuleCode) : IRequest<RuleDetailResponse>;
