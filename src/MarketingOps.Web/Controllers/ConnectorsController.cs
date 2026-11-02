using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Connectors.Commands.CompleteGoogleConnect;
using MarketingOps.Application.Connectors.Commands.DisconnectConnector;
using MarketingOps.Application.Connectors.Commands.SelectConnectorProperty;
using MarketingOps.Application.Connectors.Commands.StartGoogleConnect;
using MarketingOps.Application.Connectors.Commands.TestConnector;
using MarketingOps.Application.Connectors.Commands.TriggerConnectorSync;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Application.Connectors.Queries.GetGoogleProperties;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Sites;
using MarketingOps.Web.Filters;
using MarketingOps.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>
/// Connector bağlama akışı. Google'a tek yetkiyle bağlanılır (Search Console, GA4, GTM; salt okunur), ardından siteye uygun mülkler seçilir.
/// </summary>
[Route("sites/{siteId:guid}/settings/connectors")]
public class ConnectorsController(ISender sender) : SiteControllerBase
{
    /// <summary>Kullanıcıyı Google izin ekranına yönlendirir.</summary>
    [HttpPost("google/connect")]
    public async Task<IActionResult> ConnectGoogle(Guid siteId)
    {
        var (ok, url) = await TrySendAsync(sender, new StartGoogleConnectCommand(siteId, CallbackUrl()));
        if (ok) return HtmxRedirect(url!);
        return ErrorToast();
    }

    /// <summary>Google dönüşü. Site route'ta değil, şifreli state içindedir (Google'a tek bir dönüş adresi kaydedilir).</summary>
    [HttpGet("~/connectors/google/callback")]
    [SkipSiteContext]
    public async Task<IActionResult> GoogleCallback(string? state, string? code, string? error, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(state)) return BadRequest();
        GoogleConnectResult result;
        try
        {
            result = await sender.Send(new CompleteGoogleConnectCommand(state, code, error, CallbackUrl()), ct);
        }
        catch (GoogleApiException ex)
        {
            TempData[FlashMessage.Key] = ex.Message;
            return Redirect("/");
        }

        if (!result.Connected)
        {
            TempData[FlashMessage.Key] = result.Message;
            return Redirect(ReturnUrl(result.SiteId, finished: false));
        }
        return RedirectToAction(nameof(SelectGoogle), new { siteId = result.SiteId });
    }

    [HttpGet("google/select")]
    public async Task<IActionResult> SelectGoogle(Guid siteId, CancellationToken ct)
    {
        try
        {
            var properties = await sender.Send(new GetGooglePropertiesQuery(siteId), ct);
            ViewData["Title"] = "Google mülklerini seç";
            return View(properties);
        }
        catch (Exception ex) when (ex is ConflictException or GoogleApiException)
        {
            TempData[FlashMessage.Key] = ex.Message;
            return Redirect(ReturnUrl(siteId, finished: false));
        }
    }

    /// <summary>Formda her bölüm için bir seçenek gönderilir; "Bağlama" seçeneği boş değerdir ve mevcut mülkü kaldırır.</summary>
    [HttpPost("google/select")]
    public async Task<IActionResult> SaveGoogleSelection(Guid siteId)
    {
        string? Field(string name) => Request.Form.TryGetValue(name, out var value) ? value.ToString() : null;
        if (await TrySendAsync(sender, new SelectConnectorPropertyCommand(siteId, Field("searchConsoleSiteUrl"), Field("ga4PropertyId"))))
            return HtmxRedirect(ReturnUrl(siteId, finished: true), "Google mülk seçimi kaydedildi. Senkron başladı.");
        return ErrorToast();
    }

    [HttpPost("{type}/test")]
    public async Task<IActionResult> Test(Guid siteId, ConnectorType type)
    {
        var (ok, result) = await TrySendAsync(sender, new TestConnectorCommand(siteId, type));
        if (!ok) return ErrorToast();
        TempData[FlashMessage.Key] = result!.Message;
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }

    [HttpPost("{type}/disconnect")]
    public async Task<IActionResult> Disconnect(Guid siteId, ConnectorType type)
    {
        if (!await TrySendAsync(sender, new DisconnectConnectorCommand(siteId, type))) return ErrorToast();
        TempData[FlashMessage.Key] = $"{DisplayText.Of(type)} bağlantısı kesildi. Toplanmış veriler korunur.";
        Response.Headers["HX-Refresh"] = "true";
        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(Guid siteId)
    {
        if (!await TrySendAsync(sender, new TriggerConnectorSyncCommand(siteId))) return ErrorToast();
        Response.Toast("Senkron kuyruğa eklendi. Birkaç dakika içinde veriler güncellenir.");
        return NoContent();
    }

    /// <summary>
    /// Sihirbazın Search Console adımından gelindiyse oraya, değilse bağlantı ayarlarına döner. Çerez yalnızca seçim
    /// kaydedilince silinir; başarısız denemeden sonra tekrar denenirse yine sihirbaza dönülür.
    /// </summary>
    private string ReturnUrl(Guid siteId, bool finished)
    {
        if (Request.Cookies[SetupController.ReturnCookie] != siteId.ToString())
            return Url.Action("Index", "SiteSettings", new { siteId }) + "#baglantilar";
        if (finished) Response.Cookies.Delete(SetupController.ReturnCookie);
        return Url.Action("Step", "Setup", new { siteId, step = SiteSetupStep.SearchConsole })!;
    }

    private string CallbackUrl() => Url.Action(nameof(GoogleCallback), "Connectors", null, Request.Scheme)!;

    private NoContentResult ErrorToast()
    {
        Response.Toast(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "İşlem yapılamadı.", "error");
        return NoContent();
    }
}
