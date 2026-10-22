using System.Text.Json;

namespace MarketingOps.Web.Infrastructure;

public static class HtmxExtensions
{
    public static bool IsHtmx(this HttpRequest request) => request.Headers.ContainsKey("HX-Request");

    /// <summary>htmx isteği bir partial'ı mı yoksa sayfanın tamamını mı (hx-boost/geçmiş) bekliyor.</summary>
    public static bool IsHtmxPartial(this HttpRequest request) =>
        request.IsHtmx() && !request.Headers.ContainsKey("HX-History-Restore-Request") && !request.Headers.ContainsKey("HX-Boosted");

    /// <summary>İstemcide toast gösterir (layout'taki "toast" olay dinleyicisi).</summary>
    public static void Toast(this HttpResponse response, string message, string kind = "ok") =>
        response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new { toast = new { message, kind } });
}
