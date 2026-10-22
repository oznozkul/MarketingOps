using MediatR;

namespace MarketingOps.Application.Rules.Queries.GetRules;

/// <summary>Tüm kurallar, sitenin efektif ayarlarıyla. Kurallar kodda tanımlı ve az sayıda olduğu için sayfalanmaz.</summary>
public sealed record GetRulesQuery(Guid SiteId) : IRequest<IReadOnlyList<RuleListItemResponse>>;
