using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels.Sites;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

public class HomeController(ISiteContext context) : Controller
{
    /// <summary>Kök adres son seçilen (yoksa ilk) sitenin genel bakışına yönlendirir.</summary>
    [HttpGet("/")]
    public IActionResult Index()
    {
        if (context.Site is null)
        {
            ViewData["Title"] = "İlk sitenizi ekleyin";
            return View("NoSites", new CreateSiteForm());
        }
        return RedirectToAction("Index", "Overview", new { siteId = context.Site.Id });
    }
}
