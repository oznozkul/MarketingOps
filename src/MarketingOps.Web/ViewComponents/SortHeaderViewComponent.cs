using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewComponents;

/// <summary>Sunucu tarafı sıralanabilir tablo başlığı. Sıralama query string'de (sort, desc) tutulur.</summary>
public class SortHeaderViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(string label, string key, string? currentSort, bool descending, string target = "#results", string? cssClass = null, string? show = null)
    {
        var active = string.Equals(key, currentSort, StringComparison.OrdinalIgnoreCase);
        var nextDesc = active && !descending;
        var url = Request.With(("sort", key), ("desc", nextDesc ? "true" : null), ("page", null));
        var aria = active ? (descending ? "descending" : "ascending") : null;
        return View(new SortHeaderViewModel(label, url, aria, active, descending, target, cssClass, show));
    }
}
