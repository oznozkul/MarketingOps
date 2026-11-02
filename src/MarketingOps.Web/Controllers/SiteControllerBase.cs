using FluentValidation;
using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Domain.Common;
using MarketingOps.Web.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>/sites/{siteId}/... altındaki ekranların ortak tabanı. Site, SiteContextFilter tarafından çözülür.</summary>
[AutoValidateAntiforgeryToken]
public abstract class SiteControllerBase : Controller
{
    /// <summary>
    /// Formdan gelen komutu gönderir. Doğrulama hataları alan adına, domain ve çakışma hataları formun geneline
    /// ModelState üzerinden yazılır; böylece form aynı değerlerle hata mesajlarıyla yeniden gösterilebilir.
    /// </summary>
    protected async Task<(bool Ok, TResponse? Result)> TrySendAsync<TResponse>(ISender sender, IRequest<TResponse> request)
    {
        try
        {
            return (true, await sender.Send(request, HttpContext.RequestAborted));
        }
        catch (Exception ex) when (ex is ValidationException or DomainException or ConflictException)
        {
            RecordErrors(ex);
            return (false, default);
        }
    }

    protected async Task<bool> TrySendAsync(ISender sender, IRequest request)
    {
        try
        {
            await sender.Send(request, HttpContext.RequestAborted);
            return true;
        }
        catch (Exception ex) when (ex is ValidationException or DomainException or ConflictException)
        {
            RecordErrors(ex);
            return false;
        }
    }

    private void RecordErrors(Exception ex)
    {
        if (ex is ValidationException validation)
            foreach (var error in validation.Errors) ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        else
            ModelState.AddModelError(string.Empty, ex.Message);
    }

    /// <summary>htmx isteğini başka sayfaya yönlendirir; mesaj yeni sayfada toast olarak gösterilir.</summary>
    protected IActionResult HtmxRedirect(string url, string? flashMessage = null)
    {
        if (flashMessage is not null) TempData[FlashMessage.Key] = flashMessage;
        Response.Headers["HX-Redirect"] = url;
        return NoContent();
    }
}
