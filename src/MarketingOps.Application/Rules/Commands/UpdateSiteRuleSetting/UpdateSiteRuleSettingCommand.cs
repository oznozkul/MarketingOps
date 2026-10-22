using MarketingOps.Domain.Findings;
using MediatR;

namespace MarketingOps.Application.Rules.Commands.UpdateSiteRuleSetting;

/// <summary>Kuralın site ayarını kaydeder (yoksa oluşturur). Ayar kaydının kimliğini döner.</summary>
public sealed record UpdateSiteRuleSettingCommand(
    Guid SiteId,
    string RuleCode,
    bool IsEnabled,
    Severity? SeverityOverride,
    IReadOnlyDictionary<Guid, Severity> PageTypeSeverities,
    IReadOnlyDictionary<string, decimal> Thresholds) : IRequest<Guid>;
