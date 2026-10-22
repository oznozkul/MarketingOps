using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Commands.CancelCrawl;
using MarketingOps.Application.Crawls.Commands.StartCrawl;
using MarketingOps.Application.Crawls.Queries;
using MarketingOps.Application.Crawls.Queries.GetCrawlById;
using MarketingOps.Application.Crawls.Queries.GetCrawls;
using MarketingOps.Application.Crawls.Queries.GetPages;
using MarketingOps.Application.Findings.Commands.RequestCrawlEvaluation;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites/{siteId:guid}/crawls")]
public sealed class CrawlsApiController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<CrawlListItemResponse>> GetCrawls(Guid siteId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetCrawlsQuery(siteId, page, pageSize), ct);

    [HttpGet("{crawlId:guid}")]
    public Task<CrawlDetailResponse> GetCrawl(Guid siteId, Guid crawlId, CancellationToken ct) =>
        sender.Send(new GetCrawlByIdQuery(siteId, crawlId), ct);

    [HttpGet("{crawlId:guid}/pages")]
    public Task<PagedResponse<PageListItemResponse>> GetPages(Guid siteId, Guid crawlId, [FromQuery] Guid? pageTypeId, [FromQuery] bool unmatched,
        [FromQuery] PageStatusFilter? status, [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetPagesQuery(siteId, crawlId, pageTypeId, unmatched, status, search, page, pageSize), ct);

    /// <summary>Taramayı kuyruğa ekler; işçi süreci yürütür. Aktif tarama varsa 409.</summary>
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Start(Guid siteId, CancellationToken ct)
    {
        var id = await sender.Send(new StartCrawlCommand(siteId), ct);
        return AcceptedAtAction(nameof(GetCrawl), new { siteId, crawlId = id }, id);
    }

    [HttpPost("{crawlId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid siteId, Guid crawlId, CancellationToken ct)
    {
        await sender.Send(new CancelCrawlCommand(siteId, crawlId), ct);
        return NoContent();
    }

    /// <summary>Bitmiş ama değerlendirilmemiş taramayı kural değerlendirme kuyruğuna ekler.</summary>
    [HttpPost("{crawlId:guid}/evaluate")]
    public async Task<IActionResult> Evaluate(Guid siteId, Guid crawlId, CancellationToken ct)
    {
        await sender.Send(new RequestCrawlEvaluationCommand(siteId, crawlId), ct);
        return Accepted();
    }
}
