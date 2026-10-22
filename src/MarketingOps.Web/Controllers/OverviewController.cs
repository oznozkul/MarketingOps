using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}")]
public class OverviewController : SiteControllerBase
{
    [HttpGet("")]
    [HttpGet("overview")]
    public IActionResult Index() =>
        Placeholder("Genel Bakış", "Skorlar, kritik uyarılar, öncelikli sorunlar ve trendler bu ekranda yer alacak.", "layout-dashboard");
}
