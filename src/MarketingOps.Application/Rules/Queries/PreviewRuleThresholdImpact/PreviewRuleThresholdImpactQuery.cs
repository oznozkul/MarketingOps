using MediatR;

namespace MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;

/// <summary>Yeni eşiklerle kuralı son değerlendirilmiş taramada yeniden çalıştırır; hiçbir şey kaydetmez.</summary>
public sealed record PreviewRuleThresholdImpactQuery(Guid SiteId, string RuleCode, IReadOnlyDictionary<string, decimal> Thresholds)
    : IRequest<ThresholdImpactResponse>;
