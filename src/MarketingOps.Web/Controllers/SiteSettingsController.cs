using MarketingOps.Application.Sites.Commands.CreateSite;
using MarketingOps.Application.Sites.Commands.DeleteSite;
using MarketingOps.Application.Sites.Commands.UpdateSite;
using MarketingOps.Application.Sites.Commands.UpdateTaskKeyPrefix;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Tasks.Commands.RemoveTaskTrackerConnection;
using MarketingOps.Application.Tasks.Commands.SaveTaskTrackerConnection;
using MarketingOps.Application.Tasks.Queries.GetAiStatus;
using MarketingOps.Application.Tasks.Queries.GetTaskTrackerConnection;
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
        return View(new SiteSettingsViewModel(
            site,
            context.Sites,
            new SiteNameForm { Name = site.Name },
            CrawlSettingsForm.From(site.CrawlSettings),
            TrackingIdsForm.From(site.ExpectedTrackingIds),
            new TaskKeyPrefixForm { Prefix = site.TaskKeyPrefix },
            TaskTrackerForm.From(await sender.Send(new GetTaskTrackerConnectionQuery(siteId), ct)),
            await sender.Send(new GetAiStatusQuery(), ct)));
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
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast("Tarama ayarları kaydedildi.");
            // Kaydedilen (normalize edilmiş) değerler gösterilsin.
            form = CrawlSettingsForm.From((await sender.Send(new GetSiteByIdQuery(siteId), ct)).CrawlSettings);
        }
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

    [HttpPost("task-tracker")]
    public async Task<IActionResult> SaveTaskTracker(Guid siteId, TaskTrackerForm form)
    {
        if ((await TrySendAsync(sender, new SaveTaskTrackerConnectionCommand(siteId, form.BaseUrl, form.ProjectKey, form.IssueType, form.Email, form.ApiToken))).Ok)
        {
            TempData[FlashMessage.Key] = "Jira bağlantısı kaydedildi.";
            Response.Headers["HX-Refresh"] = "true";
            return NoContent();
        }
        form.ApiToken = null;
        return PartialView("_TaskTrackerForm", form);
    }

    [HttpPost("task-tracker/remove")]
    public async Task<IActionResult> RemoveTaskTracker(Guid siteId)
    {
        await TrySendAsync(sender, new RemoveTaskTrackerConnectionCommand(siteId));
        TempData[FlashMessage.Key] = "Jira bağlantısı kaldırıldı.";
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }
}
