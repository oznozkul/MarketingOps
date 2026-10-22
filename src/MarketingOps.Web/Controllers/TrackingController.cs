using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/tracking")]
public class TrackingController : SiteControllerBase
{
    [HttpGet("")]
    public IActionResult Index() =>
        Placeholder("Tracking Denetimi", "Sipariş ve GA4 karşılaştırması, tag denetim matrisi, event sağlığı ve Consent Mode.", "activity");
}
