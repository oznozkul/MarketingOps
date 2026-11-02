using MarketingOps.Application.Connectors.Queries.GetConnectors;
using MarketingOps.Application.Crawls.Commands.StartCrawl;
using MarketingOps.Application.Sites.Commands.CompleteSetupStep;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Sites;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Setup;
using MarketingOps.Web.ViewModels.Sites;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>
/// Yeni site sihirbazı. Domain adımı siteyi oluşturur (SiteSettingsController.Create); kalan adımlar burada.
/// Hangi adımda kalındığı sitede saklanır; sihirbaz kapatılıp sonra aynı adımdan sürdürülebilir.
/// </summary>
[Route("sites/{siteId:guid}/setup")]
public class SetupController(ISender sender) : SiteControllerBase
{
    /// <summary>Search Console adımından Google'a gidilirse bağlantı akışı ayarlar yerine sihirbaza döner.</summary>
    public const string ReturnCookie = "mo-setup-return";

    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        return site.SetupStep is { } step
            ? RedirectToAction(nameof(Step), new { siteId, step })
            : RedirectToAction("Index", "Overview", new { siteId });
    }

    [HttpGet("{step}")]
    public async Task<IActionResult> Step(Guid siteId, SiteSetupStep step, CancellationToken ct)
    {
        var site = await sender.Send(new GetSiteByIdQuery(siteId), ct);
        var model = new SetupViewModel(site, step);
        // Henüz gelinmemiş adım açılamaz; adres elle yazılsa da sıradaki adıma gidilir.
        if (!model.IsReached(step)) return RedirectToAction(nameof(Step), new { siteId, step = site.SetupStep });

        ViewData["Title"] = "Site kurulumu";
        return View(step.ToString(), step switch
        {
            SiteSetupStep.SearchConsole => await WithConnectorsAsync(model, ct),
            SiteSetupStep.Connectors => await WithTrackingAsync(model, ct),
            SiteSetupStep.CrawlSettings => model with { Crawl = CrawlSettingsForm.From(site.CrawlSettings, site.PageTypes.Count) },
            _ => model
        });
    }

    [HttpPost("{step}/complete")]
    public async Task<IActionResult> Complete(Guid siteId, SiteSetupStep step)
    {
        if (!await TrySendAsync(sender, new CompleteSetupStepCommand(siteId, step)))
        {
            Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
            return NoContent();
        }
        return HtmxRedirect(Url.Action(nameof(Step), new { siteId, step = step + 1 })!);
    }

    [HttpPost("first-crawl")]
    public async Task<IActionResult> StartFirstCrawl(Guid siteId)
    {
        var (ok, crawlId) = await TrySendAsync(sender, new StartCrawlCommand(siteId));
        if (!ok || !await TrySendAsync(sender, new CompleteSetupStepCommand(siteId, SiteSetupStep.FirstCrawl)))
        {
            Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
            return NoContent();
        }
        return HtmxRedirect(Url.Action("Detail", "Crawls", new { siteId, crawlId })!, "Kurulum tamamlandı. İlk tarama başladı.");
    }

    private async Task<SetupViewModel> WithConnectorsAsync(SetupViewModel model, CancellationToken ct)
    {
        Response.Cookies.Append(ReturnCookie, model.Site.Id.ToString(),
            new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromHours(1) });
        return model with { Connectors = await sender.Send(new GetConnectorsQuery(model.Site.Id), ct) };
    }

    private async Task<SetupViewModel> WithTrackingAsync(SetupViewModel model, CancellationToken ct)
    {
        var connectors = await sender.Send(new GetConnectorsQuery(model.Site.Id), ct);
        return model with
        {
            TrackingIds = TrackingIdsForm.From(model.Site.ExpectedTrackingIds),
            Meta = new MetaTokenForm { IsConnected = connectors.Connectors.Any(c => c.Type == ConnectorType.Meta && c.Status == ConnectorStatus.Connected) }
        };
    }
}
