using FluentValidation;
using MarketingOps.Application.PageTypes.Commands.RemovePageType;
using MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Common;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.PageTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/settings/page-types")]
public class PageTypesController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        ViewData["Title"] = "Sayfa Tipleri";
        return View(new PageTypeListViewModel(
            await sender.Send(new GetSiteByIdQuery(siteId), ct),
            await sender.Send(new GetPageTypeStatsQuery(siteId), ct)));
    }

    [HttpGet("new")]
    public async Task<IActionResult> New(Guid siteId, CancellationToken ct)
    {
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        ViewData["Title"] = "Yeni sayfa tipi";
        var form = new PageTypeForm
        {
            Conditions = [new(Domain.Sites.MatchConditionType.UrlGlob, "")],
            SampleUrls = PageTypeForm.DefaultSamples(site.Domain, site.PageTypes)
        };
        return View("Editor", new PageTypeEditorViewModel(null, form));
    }

    [HttpGet("{pageTypeId:guid}")]
    [SiteSwitchFallback(nameof(Index))]
    public async Task<IActionResult> Edit(Guid siteId, Guid pageTypeId, CancellationToken ct)
    {
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        var pageType = site.PageTypes.FirstOrDefault(p => p.Id == pageTypeId);
        if (pageType is null) return NotFound();

        ViewData["Title"] = pageType.Name;
        var form = PageTypeForm.From(pageType);
        form.SampleUrls = PageTypeForm.DefaultSamples(site.Domain, site.PageTypes);
        return View("Editor", new PageTypeEditorViewModel(pageTypeId, form));
    }

    [HttpPost("")]
    public async Task<IActionResult> Create(Guid siteId, PageTypeForm form)
    {
        if (ModelState.IsValid && (await TrySendAsync(sender, form.ToAddCommand(siteId))).Ok)
            return HtmxRedirect(Url.SiteAction(nameof(Index), "PageTypes"), $"\"{form.Name.Trim()}\" sayfa tipi eklendi.");
        return PartialView("_PageTypeForm", new PageTypeEditorViewModel(null, form));
    }

    [HttpPost("{pageTypeId:guid}")]
    public async Task<IActionResult> Update(Guid siteId, Guid pageTypeId, PageTypeForm form)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToUpdateCommand(siteId, pageTypeId)))
            return HtmxRedirect(Url.SiteAction(nameof(Index), "PageTypes"), $"\"{form.Name.Trim()}\" kaydedildi.");
        return PartialView("_PageTypeForm", new PageTypeEditorViewModel(pageTypeId, form));
    }

    [HttpPost("{pageTypeId:guid}/delete")]
    public async Task<IActionResult> Delete(Guid siteId, Guid pageTypeId)
    {
        await sender.Send(new RemovePageTypeCommand(siteId, pageTypeId), HttpContext.RequestAborted);
        return HtmxRedirect(Url.SiteAction(nameof(Index), "PageTypes"), "Sayfa tipi silindi.");
    }

    /// <summary>Canlı önizleme. Eksik ya da hatalı koşullar hata değil, panelde açıklama olarak gösterilir.</summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(Guid siteId, Guid? pageTypeId, PageTypeForm form)
    {
        try
        {
            var result = await sender.Send(form.ToPreviewQuery(siteId, pageTypeId), HttpContext.RequestAborted);
            return PartialView("_PageTypePreview", new PageTypePreviewViewModel(result, []));
        }
        catch (ValidationException ex)
        {
            return PartialView("_PageTypePreview", new PageTypePreviewViewModel(null, ex.Errors.Select(e => e.ErrorMessage).Distinct().ToList()));
        }
        catch (DomainException ex)
        {
            return PartialView("_PageTypePreview", new PageTypePreviewViewModel(null, [ex.Message]));
        }
    }
}
