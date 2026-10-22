using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Sites.Commands.CreateSite;
using MarketingOps.Application.Sites.Commands.DeleteSite;
using MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;
using MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;
using MarketingOps.Application.Sites.Commands.UpdateSite;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Sites.Queries.GetSites;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites")]
public sealed class SitesApiController(ISender sender) : ControllerBase
{
    [HttpGet]
    public Task<PagedResponse<SiteListItemResponse>> GetSites([FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default) =>
        sender.Send(new GetSitesQuery(page, pageSize), ct);

    [HttpGet("{siteId:guid}")]
    public Task<SiteDetailResponse> GetSite(Guid siteId, CancellationToken ct) =>
        sender.Send(new GetSiteByIdQuery(siteId), ct);

    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CreateSiteCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command, ct);
        return CreatedAtAction(nameof(GetSite), new { siteId = id }, id);
    }

    [HttpPut("{siteId:guid}")]
    public async Task<IActionResult> Update(Guid siteId, UpdateSiteRequest request, CancellationToken ct)
    {
        await sender.Send(new UpdateSiteCommand(siteId, request.Name), ct);
        return NoContent();
    }

    [HttpPut("{siteId:guid}/crawl-settings")]
    public async Task<IActionResult> UpdateCrawlSettings(Guid siteId, UpdateCrawlSettingsCommand command, CancellationToken ct)
    {
        await sender.Send(command with { SiteId = siteId }, ct);
        return NoContent();
    }

    [HttpPut("{siteId:guid}/expected-tracking-ids")]
    public async Task<IActionResult> UpdateExpectedTrackingIds(Guid siteId, UpdateExpectedTrackingIdsCommand command, CancellationToken ct)
    {
        await sender.Send(command with { SiteId = siteId }, ct);
        return NoContent();
    }

    [HttpDelete("{siteId:guid}")]
    public async Task<IActionResult> Delete(Guid siteId, CancellationToken ct)
    {
        await sender.Send(new DeleteSiteCommand(siteId), ct);
        return NoContent();
    }
}
