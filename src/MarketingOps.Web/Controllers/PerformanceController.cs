using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/performance")]
public class PerformanceController : SiteControllerBase
{
    [HttpGet("")]
    public IActionResult Index() =>
        Placeholder("Performans", "Core Web Vitals ve crawler'ın bulduğu performans sorunları.", "gauge");
}
