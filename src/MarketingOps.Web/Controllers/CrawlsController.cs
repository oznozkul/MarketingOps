using MarketingOps.Application.Crawls.Commands.CancelCrawl;
using MarketingOps.Application.Crawls.Commands.StartCrawl;
using MarketingOps.Application.Crawls.Queries.GetCrawlById;
using MarketingOps.Application.Crawls.Queries.GetCrawls;
using MarketingOps.Application.Crawls.Queries.GetCrawlStatus;
using MarketingOps.Application.Crawls.Queries.GetPages;
using MarketingOps.Application.Findings.Commands.RequestCrawlEvaluation;
using MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewComponents;
using MarketingOps.Web.ViewModels.Crawls;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/crawls")]
public class CrawlsController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        pageSize = PaginationViewComponent.Sizes.Contains(pageSize) ? pageSize : PaginationViewComponent.Sizes[0];
        var model = new CrawlsIndexViewModel(
            await sender.Send(new GetCrawlStatusQuery(siteId), ct),
            await sender.Send(new GetCrawlsQuery(siteId, Math.Max(1, page), pageSize), ct));

        if (Request.IsHtmxPartial()) return PartialView("_CrawlsTable", model.Crawls);
        ViewData["Title"] = "Taramalar";
        return View(model);
    }

    /// <summary>Taramayı yalnızca kuyruğa ekler; işçi süreci yürütür.</summary>
    [HttpPost("start")]
    public async Task<IActionResult> Start(Guid siteId)
    {
        var (ok, crawlId) = await TrySendAsync(sender, new StartCrawlCommand(siteId));
        if (ok) return HtmxRedirect(Url.SiteAction(nameof(Detail), "Crawls", new { crawlId }), "Tarama kuyruğa eklendi.");

        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
        return NoContent();
    }

    [HttpPost("{crawlId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid siteId, Guid crawlId)
    {
        if (await TrySendAsync(sender, new CancelCrawlCommand(siteId, crawlId)))
        {
            TempData[FlashMessage.Key] = "Tarama iptal edildi.";
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }
        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
        return NoContent();
    }

    /// <summary>Değerlendirilmemiş taramayı kural değerlendirme kuyruğuna ekler; site yeniden taranmaz.</summary>
    [HttpPost("{crawlId:guid}/evaluate")]
    public async Task<IActionResult> Evaluate(Guid siteId, Guid crawlId)
    {
        if (await TrySendAsync(sender, new RequestCrawlEvaluationCommand(siteId, crawlId)))
        {
            TempData[FlashMessage.Key] = "Kural değerlendirmesi kuyruğa eklendi. Sonuçlar birkaç saniye içinde bu sayfada görünür.";
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }
        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
        return NoContent();
    }

    [HttpGet("{crawlId:guid}")]
    [SiteSwitchFallback(nameof(Index))]
    public async Task<IActionResult> Detail(Guid siteId, Guid crawlId, Guid? pageTypeId, bool unmatched, PageStatusFilter? status, string? q,
        int page = 1, int pageSize = 25, CancellationToken ct = default)
    {
        pageSize = PaginationViewComponent.Sizes.Contains(pageSize) ? pageSize : PaginationViewComponent.Sizes[0];
        var filter = new PagesFilter(unmatched ? null : pageTypeId, unmatched, status, string.IsNullOrWhiteSpace(q) ? null : q.Trim());
        var pages = await sender.Send(new GetPagesQuery(siteId, crawlId, filter.PageTypeId, filter.Unmatched, filter.Status, filter.Q, Math.Max(1, page), pageSize), ct);

        var detail = await sender.Send(new GetCrawlByIdQuery(siteId, crawlId), ct);
        if (Request.IsHtmxPartial()) return PartialView("_PagesTable", new CrawlDetailViewModel(detail, pages, filter, []));

        var changes = detail.Crawl.EvaluatedAt is null ? [] : await sender.Send(new GetCrawlFindingChangesQuery(siteId, detail.Crawl.Number), ct);
        var model = new CrawlDetailViewModel(detail, pages, filter, changes);

        ViewData["Title"] = $"Tarama #{detail.Crawl.Number}";
        return View(model);
    }
}
