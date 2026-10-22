using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace MarketingOps.Web.Infrastructure;

public static class SiteUrlExtensions
{
    /// <summary>
    /// Geçerli isteğin {siteId} değerini taşıyan action URL'i. Endpoint routing, farklı bir action'a link üretirken
    /// route'taki siteId'yi kendiliğinden kullanmaz; bu yüzden Url.Action tek başına null dönebilir.
    /// </summary>
    public static string SiteAction(this IUrlHelper url, string action, string controller, object? values = null)
    {
        var routeValues = new RouteValueDictionary(values) { ["siteId"] = url.ActionContext.RouteData.Values["siteId"] };
        return url.Action(action, controller, routeValues)
            ?? throw new InvalidOperationException($"{controller}.{action} için URL üretilemedi.");
    }
}
