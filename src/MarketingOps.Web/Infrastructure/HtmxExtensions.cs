using System.Text.Json;

namespace MarketingOps.Web.Infrastructure;

public static class HtmxExtensions
{
    public static bool IsHtmx(this HttpRequest request) => request.Headers.ContainsKey("HX-Request");

    /// <summary>htmx isteği bir partial'ı mı yoksa sayfanın tamamını mı (hx-boost/geçmiş) bekliyor.</summary>
    public static bool IsHtmxPartial(this HttpRequest request) =>
        request.IsHtmx() && !request.Headers.ContainsKey("HX-History-Restore-Request") && !request.Headers.ContainsKey("HX-Boosted");

    /// <summary>
    /// İstemcide toast gösterir (layout'taki "toast" olay dinleyicisi). <paramref name="alsoTrigger"/> aynı başlıkta
    /// ikinci bir olay tetikler; HX-Trigger tek başlık olduğu için ikisi birlikte yazılır.
    /// </summary>
    public static void Toast(this HttpResponse response, string message, string kind = "ok", string? alsoTrigger = null)
    {
        var events = new Dictionary<string, object> { ["toast"] = new { message, kind } };
        if (alsoTrigger is not null) events[alsoTrigger] = true;
        response.Headers["HX-Trigger"] = JsonSerializer.Serialize(events);
    }
}
