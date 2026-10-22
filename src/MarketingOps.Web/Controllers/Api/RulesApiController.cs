using MarketingOps.Application.Rules.Commands.UpdateSiteRuleSetting;
using MarketingOps.Application.Rules.Queries.GetRule;
using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites/{siteId:guid}/rules")]
public sealed class RulesApiController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyList<RuleListItemResponse>> GetRules(Guid siteId, CancellationToken ct) => sender.Send(new GetRulesQuery(siteId), ct);

    [HttpGet("{code}")]
    public Task<RuleDetailResponse> GetRule(Guid siteId, string code, CancellationToken ct) => sender.Send(new GetRuleQuery(siteId, code), ct);

    [HttpPut("{code}")]
    public Task<Guid> Update(Guid siteId, string code, UpdateRuleSettingRequest request, CancellationToken ct) =>
        sender.Send(new UpdateSiteRuleSettingCommand(siteId, code, request.IsEnabled, request.SeverityOverride,
            request.PageTypeSeverities ?? new Dictionary<Guid, Severity>(), request.Thresholds ?? new Dictionary<string, decimal>()), ct);

    /// <summary>Yeni eşiklerin son değerlendirilmiş taramadaki etkisi; hiçbir şey kaydetmez.</summary>
    [HttpPost("{code}/impact")]
    public Task<ThresholdImpactResponse> Impact(Guid siteId, string code, ThresholdImpactRequest request, CancellationToken ct) =>
        sender.Send(new PreviewRuleThresholdImpactQuery(siteId, code, request.Thresholds), ct);
}
