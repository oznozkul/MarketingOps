using MarketingOps.Application.Connectors.Queries.GetConnectors;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Tracking;
using MarketingOps.Application.Tracking.Queries.GetEventHealth;
using MarketingOps.Application.Tracking.Queries.GetOrderComparison;
using MarketingOps.Application.Tracking.Queries.GetTagAuditCell;
using MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;
using MarketingOps.Application.Tracking.Queries.GetTrackingScore;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Tracking;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Tracking;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/tracking")]
public class TrackingController(ISender sender, ITrackingJobScheduler jobs) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        ViewData["Title"] = "Tracking Denetimi";
        var connectors = await sender.Send(new GetConnectorsQuery(siteId), ct);
        return View(new TrackingPageViewModel(
            await sender.Send(new GetSiteByIdQuery(siteId), ct),
            await sender.Send(new GetTrackingScoreQuery(siteId), ct),
            await sender.Send(new GetTagAuditMatrixQuery(siteId), ct),
            await sender.Send(new GetEventHealthQuery(siteId), ct),
            await sender.Send(new GetOrderComparisonQuery(siteId), ct),
            connectors.Connectors.Any(c => c.Type == ConnectorType.Meta && c.Status == ConnectorStatus.Connected)));
    }

    [HttpPost("start")]
    public async Task<IActionResult> Start(Guid siteId, CancellationToken ct)
    {
        var matrix = await sender.Send(new GetTagAuditMatrixQuery(siteId), ct);
        if (matrix.Status == AuditStatus.Running)
        {
            ModelState.AddModelError(string.Empty, "Bu sitede bir denetim zaten çalışıyor.");
            return PartialView("_StartForm");
        }

        jobs.Enqueue(siteId);
        return HtmxRedirect(Url.SiteAction("Index", "Tracking")!, "Denetim kuyruğa eklendi.");
    }

    [HttpGet("cells")]
    public async Task<IActionResult> Cell(Guid siteId, string pageLabel, TagVendor vendor, CancellationToken ct) =>
        PartialView("_Cell", await sender.Send(new GetTagAuditCellQuery(siteId, pageLabel, vendor), ct));

    [HttpGet("events/{name}")]
    public async Task<IActionResult> Event(Guid siteId, string name, CancellationToken ct)
    {
        var health = await sender.Send(new GetEventHealthQuery(siteId), ct);
        var row = health.Events.FirstOrDefault(e => e.Name == name);
        return row is null ? NotFound() : PartialView("_EventChart", row);
    }
}
