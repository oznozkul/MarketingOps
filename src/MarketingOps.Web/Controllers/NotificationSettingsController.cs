using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

[Route("sites/{siteId:guid}/settings/notifications")]
public class NotificationSettingsController : SiteControllerBase
{
    [HttpGet("")]
    public IActionResult Index() =>
        Placeholder("Bildirimler", "Kanallar, bildirim kuralları ve yönetici özet raporları.", "bell");
}
