using MarketingOps.Application.Notifications.Commands.SendTestNotification;
using MarketingOps.Application.Notifications.Queries.GetNotificationSettings;
using MarketingOps.Application.Notifications.Queries.GetReportPreview;
using MarketingOps.Domain.Notifications;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Notifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/settings/notifications")]
public class NotificationSettingsController(ISender sender) : SiteControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> Index(Guid siteId, CancellationToken ct)
    {
        var settings = await sender.Send(new GetNotificationSettingsQuery(siteId), ct);
        var channels = NotificationChannelsForm.From(settings);
        ViewData["Title"] = "Bildirimler";
        return View(new NotificationSettingsViewModel(
            channels,
            settings.Rules.Select(r => NotificationRuleForm.From(r, channels)).ToList(),
            settings.Reports.Select(ReportScheduleForm.From).ToList()));
    }

    [HttpPost("channels")]
    public async Task<IActionResult> SaveChannels(Guid siteId, NotificationChannelsForm form, CancellationToken ct)
    {
        var saved = ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId));
        var current = NotificationChannelsForm.From(await sender.Send(new GetNotificationSettingsQuery(siteId), ct));
        if (!saved)
            return PartialView("_ChannelsForm", WithState(form, current));

        // Kanal durumu kural kartlarındaki "bağlı değil" uyarılarını da değiştirir; sayfa yenilenir.
        TempData[FlashMessage.Key] = "Kanallar kaydedildi.";
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }

    [HttpPost("rules/{eventType}")]
    public async Task<IActionResult> SaveRule(Guid siteId, NotificationEventType eventType, NotificationRuleForm form, CancellationToken ct)
    {
        form.EventType = eventType;
        var saved = ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId));
        var settings = await sender.Send(new GetNotificationSettingsQuery(siteId), ct);
        var channels = NotificationChannelsForm.From(settings);
        if (saved)
        {
            Response.Toast($"{DisplayText.Of(eventType)} kuralı kaydedildi.");
            // Kaydedilen (normalize edilmiş) alıcılar gösterilsin.
            form = NotificationRuleForm.From(settings.Rules.Single(r => r.EventType == eventType), channels);
        }
        form.Available = channels;
        return PartialView("_RuleForm", form);
    }

    [HttpPost("reports/{period}")]
    public async Task<IActionResult> SaveReport(Guid siteId, ReportPeriod period, ReportScheduleForm form, CancellationToken ct)
    {
        form.Period = period;
        if (ModelState.IsValid && await TrySendAsync(sender, form.ToCommand(siteId)))
        {
            Response.Toast($"{DisplayText.Of(period)} özet kaydedildi.");
            var settings = await sender.Send(new GetNotificationSettingsQuery(siteId), ct);
            form = ReportScheduleForm.From(settings.Reports.Single(r => r.Period == period));
        }
        return PartialView("_ReportForm", form);
    }

    /// <summary>E-postanın kendisi; ekranda yalıtılmış bir iframe içinde gösterilir.</summary>
    [HttpGet("reports/{period}/preview")]
    public async Task<IActionResult> PreviewReport(Guid siteId, ReportPeriod period, CancellationToken ct) =>
        Content(await sender.Send(new GetReportPreviewQuery(siteId, period), ct), "text/html; charset=utf-8");

    [HttpPost("test/{channel}")]
    public async Task<IActionResult> SendTest(Guid siteId, NotificationChannelKind channel)
    {
        var (ok, message) = await TrySendAsync(sender, new SendTestNotificationCommand(siteId, channel));
        if (ok)
            // Panel içi test bildirimi zildeki listeye hemen düşsün.
            Response.Toast(message!, alsoTrigger: channel == NotificationChannelKind.InApp ? "mo:notifications-changed" : null);
        else
            Response.Toast(ModelState.Values.SelectMany(v => v.Errors).First().ErrorMessage, "error");
        return NoContent();
    }

    private static NotificationChannelsForm WithState(NotificationChannelsForm form, NotificationChannelsForm current)
    {
        form.HasTeams = current.HasTeams;
        form.HasSlack = current.HasSlack;
        form.MailConfigured = current.MailConfigured;
        return form;
    }
}
