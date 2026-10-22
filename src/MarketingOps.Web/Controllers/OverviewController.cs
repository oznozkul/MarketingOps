using MarketingOps.Application.Overview.Queries.GetOverview;
using MarketingOps.Application.Scores.Queries.GetScoreBreakdown;
using MarketingOps.Domain.Scores;
using MarketingOps.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}")]
public class OverviewController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    [HttpGet("overview")]
    public async Task<IActionResult> Index(Guid siteId, int days, CancellationToken ct)
    {
        ViewData["Title"] = "Genel Bakış";
        return View(await sender.Send(new GetOverviewQuery(siteId, days == 7 ? 7 : 30), ct));
    }

    [HttpGet("overview/breakdown")]
    public async Task<IActionResult> Breakdown(Guid siteId, ScoreCategory category, CancellationToken ct) =>
        PartialView("_Breakdown", await sender.Send(new GetScoreBreakdownQuery(siteId, category), ct));
}
