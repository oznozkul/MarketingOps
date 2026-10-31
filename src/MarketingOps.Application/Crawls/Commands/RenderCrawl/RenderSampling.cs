using MarketingOps.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Commands.RenderCrawl;

/// <summary>
/// Render edilecek sayfaların seçimi: her sayfa tipinden, o taramada doğrudan 2xx dönen HTML sayfalar URL uzunluğuna göre
/// sıralanıp eşit aralıklarla alınır (hep en kısa adresler değil, tipin farklı örnekleri). Ana sayfa her zaman eklenir.
/// Hiçbir sayfa tipine uymayan sayfalar render edilmez.
/// </summary>
public static class RenderSampling
{
    public static async Task<IReadOnlyList<RenderCandidate>> LoadCandidatesAsync(IAppDbContext db, Guid crawlId, CancellationToken cancellationToken)
    {
        var rows = await db.PageSnapshots.AsNoTracking()
            .Where(p => p.CrawlId == crawlId && p.StatusCode >= 200 && p.StatusCode < 300 && p.ContentType != null && p.ContentType.Contains("html"))
            .Select(p => new { p.Url, p.PageTypeId, p.RedirectChain })
            .ToListAsync(cancellationToken);
        return rows.Where(r => r.RedirectChain.Count == 0).Select(r => new RenderCandidate(r.Url, r.PageTypeId)).ToList();
    }

    public static IReadOnlyList<RenderCandidate> Select(IReadOnlyList<RenderCandidate> pages, int perPageType)
    {
        var selected = pages
            .Where(p => p.PageTypeId is not null)
            .GroupBy(p => p.PageTypeId)
            .SelectMany(g => Spread(g.OrderBy(p => p.Url.Length).ThenBy(p => p.Url, StringComparer.Ordinal).ToList(), perPageType))
            .ToList();
        var home = pages.FirstOrDefault(p => Uri.TryCreate(p.Url, UriKind.Absolute, out var u) && u.AbsolutePath == "/" && u.Query.Length == 0);
        if (home is not null && selected.All(p => p.Url != home.Url)) selected.Insert(0, home);
        return selected;
    }

    private static IEnumerable<RenderCandidate> Spread(IReadOnlyList<RenderCandidate> sorted, int count)
    {
        if (sorted.Count <= count) return sorted;
        if (count == 1) return [sorted[0]];
        return Enumerable.Range(0, count).Select(i => sorted[(int)Math.Round(i * (sorted.Count - 1) / (double)(count - 1), MidpointRounding.AwayFromZero)]).Distinct();
    }
}
