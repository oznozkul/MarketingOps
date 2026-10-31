using MarketingOps.Application.Connectors.Commands.ConnectMeta;
using MarketingOps.Application.Connectors.Queries.GetConnectors;
using MarketingOps.Application.Sites.Commands.CreateSite;
using MarketingOps.Domain.Connectors;
using MarketingOps.Application.Sites.Commands.DeleteSite;
using MarketingOps.Application.Sites.Commands.UpdateSite;
using MarketingOps.Application.Sites.Commands.UpdateTaskKeyPrefix;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Tasks.Queries.GetAiStatus;

using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Sites;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/settings/sites")]
public class SiteSettingsController(ISender sender, ISiteContext context) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        ViewData["Title"] = "Siteler";
        var connectors = await sender.Send(new GetConnectorsQuery(siteId), ct);
        return View(new SiteSettingsViewModel(
            site,
            context.Sites,
            new SiteNameForm { Name = site.Name },
            CrawlSettingsForm.From(site.CrawlSettings, site.PageTypes.Count),
            TrackingIdsForm.From(site.ExpectedTrackingIds),
            TrackingJourneyForm.From(site.Journey),
            OrderSourceForm.From(site.OrderSource),
            new MetaTokenForm { IsConnected = connectors.Connectors.Any(c => c.Type == ConnectorType.Meta && c.Status == ConnectorStatus.Connected) },
            new TaskKeyPrefixForm { Prefix = site.TaskKeyPrefix },
            await sender.Send(new GetAiStatusQuery(), ct),
            connectors));
    }

    /// <summary>Site kimliği olmayan adres: henüz hiç site yokken de site eklenebilsin.</summary>
    [HttpPost("~/sites/new")]
    public async Task<IActionResult> Create(CreateSiteForm form)
    {
        if (ModelState.IsValid)
        {
            var (ok, id) = await TrySendAsync(sender, new CreateSiteCommand(form.Name, form.Domain));
            if (ok) return HtmxRedirect(Url.Action(nameof(Index), "SiteSettings", new { siteId = id })!, "Site eklendi.");
        }
        return PartialView("_CreateSiteForm", form);
    }

    [HttpPost("name")]
    public async Task<IActionResult> Rename(Guid siteId, SiteNameForm form)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, new UpdateSiteCommand(siteId, form.Name)))
        {
            // Ad üst barda ve site seçicide de görünür; sayfanın tamamı yenilenir.
            TempData[FlashMessage.Key] = "Site adı güncellendi.";
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }
        return PartialView("_SiteNameForm", form);
    }

    [HttpPost("crawl-settings")]
    public async Task<IActionResult> UpdateCrawlSettings(Guid siteId, CrawlSettingsForm form, CancellationToken ct)
    {
        var saved = ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId));
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        if (saved)
        {
            Response.Toast("Tarama ayarları kaydedildi.");
            // Kaydedilen (normalize edilmiş) değerler gösterilsin.
            form = CrawlSettingsForm.From(site.CrawlSettings, site.PageTypes.Count);
        }
        form.PageTypeCount = site.PageTypes.Count;
        return PartialView("_CrawlSettingsForm", form);
    }

    [HttpPost("tracking-ids")]
    public async Task<IActionResult> UpdateTrackingIds(Guid siteId, TrackingIdsForm form, CancellationToken ct)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast("Beklenen kimlikler kaydedildi.");
            form = TrackingIdsForm.From((await sender.Send(new GetSiteByIdQuery(siteId), ct)).ExpectedTrackingIds);
        }
        return PartialView("_TrackingIdsForm", form);
    }

    [HttpPost("journey")]
    public async Task<IActionResult> UpdateJourney(Guid siteId, TrackingJourneyForm form, CancellationToken ct)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast("Denetim adresleri kaydedildi.");
            form = TrackingJourneyForm.From((await sender.Send(new GetSiteByIdQuery(siteId), ct)).Journey);
        }
        return PartialView("_TrackingJourneyForm", form);
    }

    [HttpPost("order-source")]
    public async Task<IActionResult> UpdateOrderSource(Guid siteId, OrderSourceForm form, CancellationToken ct)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast("Sipariş kaynağı kaydedildi.");
            form = OrderSourceForm.From((await sender.Send(new GetSiteByIdQuery(siteId), ct)).OrderSource);
        }
        return PartialView("_OrderSourceForm", form);
    }

    [HttpPost("meta")]
    public async Task<IActionResult> UpdateMeta(Guid siteId, MetaTokenForm form, CancellationToken ct)
    {
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast(form.Remove ? "Meta bağlantısı kaldırıldı." : "Meta token'ı kaydedildi.");
            var connectors = await sender.Send(new GetConnectorsQuery(siteId), ct);
            form = new MetaTokenForm { IsConnected = connectors.Connectors.Any(c => c.Type == ConnectorType.Meta && c.Status == ConnectorStatus.Connected) };
        }
        return PartialView("_MetaTokenForm", form);
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete(Guid siteId, DeleteSiteForm form)
    {
        var domain = context.Site!.Domain;
        if (!string.Equals(form.ConfirmDomain?.Trim(), domain, StringComparison.OrdinalIgnoreCase))
            ModelState.AddModelError(nameof(DeleteSiteForm.ConfirmDomain), $"Onaylamak için {domain} yazın.");
        else if (await TrySendAsync(sender, new DeleteSiteCommand(siteId)))
            return HtmxRedirect("/", $"{domain} silindi.");

        ViewData["Domain"] = domain;
        return PartialView("_DeleteSiteForm", form);
    }

    [HttpPost("task-key-prefix")]
    public async Task<IActionResult> UpdateTaskKeyPrefix(Guid siteId, TaskKeyPrefixForm form)
    {
        if (await TrySendAsync(sender, new UpdateTaskKeyPrefixCommand(siteId, form.Prefix)))
            Response.Toast("Görev öneki kaydedildi. Yeni görevler bu önekle açılır.");
        return PartialView("_TaskKeyPrefixForm", form);
    }

}
