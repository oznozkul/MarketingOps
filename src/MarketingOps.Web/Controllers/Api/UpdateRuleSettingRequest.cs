using MarketingOps.Domain.Findings;

namespace MarketingOps.Web.Controllers.Api;

public sealed record UpdateRuleSettingRequest(
    bool IsEnabled,
    Severity? SeverityOverride,
    IReadOnlyDictionary<Guid, Severity>? PageTypeSeverities,
    IReadOnlyDictionary<string, decimal>? Thresholds);
