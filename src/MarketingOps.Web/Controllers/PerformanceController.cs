using MarketingOps.Application.Connectors.Commands.TriggerConnectorSync;
using MarketingOps.Application.Metrics.Queries.GetWebVitals;
using MarketingOps.Application.Rules.Queries.GetRules;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Performance;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/performance")]
public class PerformanceController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, DeviceStrategy device = DeviceStrategy.Mobile, CancellationToken ct = default)
    {
        var vitals = await sender.Send(new GetWebVitalsQuery(siteId, device), ct);
        var rules = (await sender.Send(new GetRulesQuery(siteId), ct)).Where(r => r.Category == FindingCategory.Performance).ToList();
        ViewData["Title"] = "Performans";
        return View(new PerformanceViewModel(device, vitals, rules));
    }

    /// <summary>Örnek sayfaları PageSpeed Insights ile ölçer (Google sayfaları açar).</summary>
    [HttpPost("measure")]
    public async Task<IActionResult> Measure(Guid siteId)
    {
        if (await TrySendAsync(sender, new TriggerConnectorSyncCommand(siteId, IncludeWebVitals: true)))
            Response.Toast("Ölçüm kuyruğa eklendi. Sayfa başına yaklaşık yarım dakika sürer; bitince sayfayı yenileyin.");
        else
            Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
        return NoContent();
    }
}
