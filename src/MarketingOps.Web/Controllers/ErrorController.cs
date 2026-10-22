using MarketingOps.Web.Filters;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[SkipSiteContext]
public class ErrorController : Controller
{
    [Route("error/{code:int?}")]
    public IActionResult Index(int? code)
    {
        Response.StatusCode = code ?? 500;
        ViewData["Title"] = code == 404 ? "Sayfa bulunamadı" : "Bir hata oluştu";
        return View("Error", code ?? 500);
    }
}
