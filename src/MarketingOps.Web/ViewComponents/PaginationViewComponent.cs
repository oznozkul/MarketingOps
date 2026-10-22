using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewComponents;

/// <summary>Sunucu tarafı sayfalama: sayfa numaraları ve sayfa başına kayıt (25/50/100).</summary>
public class PaginationViewComponent : ViewComponent
{
    public IViewComponentResult Invoke(int page, int pageSize, int total, string target = "#results", string itemLabel = "kayıt")
    {
        var pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        string UrlFor(int p) => Request.With(("page", p == 1 ? null : p.ToString()));

        var numbers = new SortedSet<int> { 1, pageCount };
        for (var p = page - 1; p <= page + 1; p++) if (p >= 1 && p <= pageCount) numbers.Add(p);
        var links = new List<PageLink>();
        var prev = 0;
        foreach (var n in numbers)
        {
            if (prev != 0 && n - prev > 1) links.Add(new PageLink(null, null, false));
            links.Add(new PageLink(n, UrlFor(n), n == page));
            prev = n;
        }

        var first = total == 0 ? 0 : (page - 1) * pageSize + 1;
        return View(new PaginationViewModel(
            total, first, Math.Min(page * pageSize, total), pageSize,
            page > 1 ? UrlFor(page - 1) : null,
            page < pageCount ? UrlFor(page + 1) : null,
            links,
            Request.With(("page", null), ("pageSize", null)),
            target, itemLabel));
    }

    /// <summary>Sayfa başına kayıt seçenekleri. Üst sınır Application'daki PagingRules.MaxPageSize ile uyumludur.</summary>
    public static IReadOnlyList<int> Sizes { get; } = [25, 50, 100];
}
