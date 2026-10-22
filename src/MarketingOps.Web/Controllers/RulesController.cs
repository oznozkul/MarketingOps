using MarketingOps.Application.Rules.Commands.SetRuleEnabled;
using MarketingOps.Application.Rules.Commands.UpdateSiteRuleSetting;
using MarketingOps.Application.Rules.Queries.GetRule;
using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Rules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/settings/rules")]
public class RulesController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        ViewData["Title"] = "Kurallar";
        return View(await sender.Send(new GetRulesQuery(siteId), ct));
    }

    /// <summary>Listedeki aç/kapa anahtarı: satır yeniden çizilir.</summary>
    [HttpPost("{code}/enabled")]
    public async Task<IActionResult> SetEnabled(Guid siteId, string code, bool isEnabled, CancellationToken ct)
    {
        if ((await TrySendAsync(sender, new SetRuleEnabledCommand(siteId, code, isEnabled))).Ok)
            Response.Toast(isEnabled ? "Kural açıldı. Sonraki taramada çalışır." : "Kural kapatıldı. Açtığı bulgular olduğu gibi kalır.");
        else
            Response.Toast(ModelState.FirstError(string.Empty) ?? ModelState.FirstError("RuleCode") ?? "Kural güncellenemedi.", "error");

        var rule = (await sender.Send(new GetRulesQuery(siteId), ct)).First(r => r.Code == code);
        return PartialView("_RuleRow", rule);
    }

    [HttpGet("{code}")]
    [SiteSwitchFallback(nameof(Index))]
    public async Task<IActionResult> Detail(Guid siteId, string code, CancellationToken ct)
    {
        var detail = await sender.Send(new GetRuleQuery(siteId, code), ct);
        ViewData["Title"] = detail.Rule.Name;
        return View(new RuleDetailViewModel(detail, RuleSettingsForm.From(detail)));
    }

    [HttpPost("{code}")]
    public async Task<IActionResult> Update(Guid siteId, string code, RuleSettingsForm form, CancellationToken ct)
    {
        var thresholds = form.ParseThresholds(ModelState);
        if (ModelState.IsValid && (await TrySendAsync(sender, new UpdateSiteRuleSettingCommand(
                siteId, code, form.IsEnabled, form.SeverityOverride, form.ParsePageTypeSeverities(), thresholds))).Ok)
        {
            TempData[FlashMessage.Key] = "Kural ayarları kaydedildi. Değişiklikler sonraki taramada uygulanır.";
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }

        var detail = await sender.Send(new GetRuleQuery(siteId, code), ct);
        return PartialView("_RuleSettingsForm", new RuleDetailViewModel(detail, form));
    }

    /// <summary>Eşik alanları değiştikçe çağrılır; hiçbir şey kaydetmez.</summary>
    [HttpGet("{code}/impact")]
    public async Task<IActionResult> Impact(Guid siteId, string code, RuleSettingsForm form, CancellationToken ct)
    {
        var thresholds = form.ParseThresholds(ModelState);
        if (!ModelState.IsValid) return PartialView("_ThresholdImpact", new ThresholdImpactViewModel(null, false, "Eşik değerleri sayı olmalı."));

        var (ok, impact) = await TrySendAsync(sender, new PreviewRuleThresholdImpactQuery(siteId, code, thresholds));
        if (!ok) return PartialView("_ThresholdImpact", new ThresholdImpactViewModel(null, false, ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage));

        var saved = (await sender.Send(new GetRuleQuery(siteId, code), ct)).Thresholds;
        var changed = saved.Any(t => thresholds.GetValueOrDefault(t.Key, t.Default) != t.Value);
        return PartialView("_ThresholdImpact", new ThresholdImpactViewModel(impact, changed, null));
    }
}
