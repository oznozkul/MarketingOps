using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Web.Filters;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[SkipSiteContext]
public class ErrorController : Controller
{
    [Route("error/{code:int?}")]
    public IActionResult Index(int? code)
    {
        // İstisnadan gelindiyse kod yoktur; bulunamayan kayıt 500 değil 404 sayfasıdır.
        var status = code ?? (HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error is NotFoundException ? 404 : 500);
        Response.StatusCode = status;
        ViewData["Title"] = status == 404 ? "Sayfa bulunamadı" : "Bir hata oluştu";
        return View("Error", status);
    }
}
