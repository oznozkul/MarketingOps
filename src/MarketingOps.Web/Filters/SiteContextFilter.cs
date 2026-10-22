using MarketingOps.Application.Common.Validation;
using MarketingOps.Application.Crawls.Queries.GetCrawlStatus;
using MarketingOps.Application.Sites.Queries.GetSites;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MarketingOps.Web.Filters;

/// <summary>
/// Route'taki {siteId}'yi çözer, <see cref="ISiteContext"/>'i doldurur ve layout için <see cref="ShellViewModel"/> üretir.
/// Route'ta site yoksa (ör. /_design) son seçilen site, o da yoksa ilk site kullanılır.
/// </summary>
public class SiteContextFilter(ISender sender, SiteContext context, LinkGenerator links) : IAsyncActionFilter
{
    public const string LastSiteCookie = "mo-site";

    public async Task OnActionExecutionAsync(ActionExecutingContext ctx, ActionExecutionDelegate next)
    {
        var skip = ctx.ActionDescriptor.EndpointMetadata.OfType<SkipSiteContextAttribute>().Any()
                   || ctx.ActionDescriptor.EndpointMetadata.OfType<ApiControllerAttribute>().Any();
        if (skip)
        {
            await next();
            return;
        }

        // Organizasyon başına site sayısı küçük; seçici için tek sayfa yeterli.
        var sites = (await sender.Send(new GetSitesQuery(1, PagingRules.MaxPageSize), ctx.HttpContext.RequestAborted)).Items;
        context.Sites = sites;

        var hasRouteSite = ctx.RouteData.Values.TryGetValue("siteId", out var raw);
        var site = hasRouteSite
            ? sites.FirstOrDefault(s => s.Id.ToString() == raw?.ToString())
            : sites.FirstOrDefault(s => s.Id.ToString() == ctx.HttpContext.Request.Cookies[LastSiteCookie]) ?? sites.FirstOrDefault();

        if (hasRouteSite && site is null)
        {
            ctx.Result = new NotFoundResult();
            return;
        }

        context.Site = site;
        if (site is not null && hasRouteSite)
            ctx.HttpContext.Response.Cookies.Append(LastSiteCookie, site.Id.ToString(),
                new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax, MaxAge = TimeSpan.FromDays(180) });

        if (site is not null && ctx.Controller is Controller controller && !ctx.HttpContext.Request.IsHtmxPartial())
        {
            var section = ctx.ActionDescriptor is ControllerActionDescriptor cad ? cad.ControllerName : "";
            var crawlStatus = await sender.Send(new GetCrawlStatusQuery(site.Id), ctx.HttpContext.RequestAborted);
            controller.ViewData[ShellViewDataExtensions.Key] = new ShellViewModel(
                site,
                sites.Select(s => new SiteSwitchItem(s, SwitchUrl(ctx, s.Id, hasRouteSite), s.Id == site.Id)).ToList(),
                crawlStatus,
                section,
                CurrentUser.Name,
                CurrentUser.Role);
        }

        await next();
    }

    /// <summary>Aynı ekranın diğer siteye ait sürümü; siteye özgü detay ekranlarında listeye düşer.</summary>
    private string SwitchUrl(ActionExecutingContext ctx, Guid siteId, bool hasRouteSite)
    {
        var http = ctx.HttpContext;
        if (!hasRouteSite || ctx.ActionDescriptor is not ControllerActionDescriptor cad)
            return links.GetPathByAction(http, "Index", "Overview", new { siteId }) ?? "/";

        var fallback = cad.MethodInfo.GetCustomAttributes(typeof(SiteSwitchFallbackAttribute), false)
            .OfType<SiteSwitchFallbackAttribute>().FirstOrDefault();
        if (fallback is not null)
            return links.GetPathByAction(http, fallback.Action, cad.ControllerName, new { siteId }) ?? "/";

        var values = new RouteValueDictionary(ctx.RouteData.Values) { ["siteId"] = siteId };
        return links.GetPathByAction(http, cad.ActionName, cad.ControllerName, values) ?? "/";
    }
}
