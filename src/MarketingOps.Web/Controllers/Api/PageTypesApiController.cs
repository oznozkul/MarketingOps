using MarketingOps.Application.PageTypes.Commands.AddPageType;
using MarketingOps.Application.PageTypes.Commands.RemovePageType;
using MarketingOps.Application.PageTypes.Commands.UpdatePageType;
using MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers.Api;

[ApiController]
[Route("api/sites/{siteId:guid}/page-types")]
public sealed class PageTypesApiController(ISender sender) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<Guid>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(Guid siteId, AddPageTypeCommand command, CancellationToken ct)
    {
        var id = await sender.Send(command with { SiteId = siteId }, ct);
        return Created($"/api/sites/{siteId}/page-types/{id}", id);
    }

    [HttpPut("{pageTypeId:guid}")]
    public async Task<IActionResult> Update(Guid siteId, Guid pageTypeId, UpdatePageTypeCommand command, CancellationToken ct)
    {
        await sender.Send(command with { SiteId = siteId, PageTypeId = pageTypeId }, ct);
        return NoContent();
    }

    [HttpDelete("{pageTypeId:guid}")]
    public async Task<IActionResult> Remove(Guid siteId, Guid pageTypeId, CancellationToken ct)
    {
        await sender.Send(new RemovePageTypeCommand(siteId, pageTypeId), ct);
        return NoContent();
    }

    /// <summary>Kaydetmeden önizleme: koşulların örnek URL'lerden hangileriyle eşleştiği.</summary>
    [HttpPost("preview")]
    public Task<PageTypeMatchPreviewResponse> Preview(Guid siteId, PreviewPageTypeMatchQuery query, CancellationToken ct) =>
        sender.Send(query with { SiteId = siteId }, ct);
}
