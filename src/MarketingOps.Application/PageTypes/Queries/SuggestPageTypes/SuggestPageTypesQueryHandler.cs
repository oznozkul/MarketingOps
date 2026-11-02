using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.PageTypes.Suggestions;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Sites;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.PageTypes.Queries.SuggestPageTypes;

public sealed class SuggestPageTypesQueryHandler(
    IAppDbContext db,
    ISitemapSampler sitemaps,
    ILogger<SuggestPageTypesQueryHandler> logger,
    IAiSuggestionService? ai = null) : IRequestHandler<SuggestPageTypesQuery, PageTypeSuggestionsResponse>
{
    private const int SamplesPerSuggestion = 5;

    public async Task<PageTypeSuggestionsResponse> Handle(SuggestPageTypesQuery request, CancellationToken cancellationToken)
    {
        var site = await db.Sites.AsNoTracking()
            .Where(s => s.Id == request.SiteId)
            .Select(s => new { s.Domain, PageTypes = s.PageTypes.Select(p => p.Name).ToList() })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Site bulunamadı.");

        var urls = request.Source == PageTypeSuggestionSource.Sitemap
            ? await sitemaps.GetUrlsAsync(site.Domain, cancellationToken)
            : await UnmatchedUrlsAsync(request.SiteId, cancellationToken);
        var groups = UrlGrouping.Group(urls);
        if (ai is null || groups.Count == 0)
            return new PageTypeSuggestionsResponse(request.Source, ai is not null, urls.Count, groups, [], null);

        try
        {
            var raw = await ai.SuggestPageTypesAsync(new PageTypeSuggestionRequest(site.Domain, groups, site.PageTypes), cancellationToken);
            return new PageTypeSuggestionsResponse(request.Source, true, urls.Count, groups, Measure(raw, urls, site.PageTypes), null);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Sayfa tipi önerisi alınamadı ({Domain}).", site.Domain);
            return new PageTypeSuggestionsResponse(request.Source, true, urls.Count, groups, [], "AI önerisi alınamadı. URL grupları aşağıda; sayfa tiplerini elle ekleyebilirsiniz.");
        }
    }

    /// <summary>Desen gerçek adreslere uygulanır: hiçbir sayfaya uymayan, geçersiz ya da mevcut tiple aynı adlı öneri atılır.</summary>
    private static List<PageTypeSuggestionResponse> Measure(IReadOnlyList<AiPageTypeSuggestion> raw, IReadOnlyList<string> urls, IReadOnlyList<string> existing)
    {
        var uris = urls.Select(u => Uri.TryCreate(u, UriKind.Absolute, out var uri) ? uri : null).OfType<Uri>().ToList();
        var result = new List<PageTypeSuggestionResponse>();
        foreach (var s in raw.DistinctBy(s => s.Name.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(s.Name) || existing.Contains(s.Name.Trim(), StringComparer.OrdinalIgnoreCase)) continue;
            MatchCondition condition;
            try
            {
                condition = new MatchCondition(MatchConditionType.UrlGlob, s.UrlGlob);
            }
            catch (DomainException)
            {
                continue;
            }
            var matches = uris.Where(u => condition.IsMatch(u, [])).ToList();
            if (matches.Count == 0) continue;
            result.Add(new PageTypeSuggestionResponse(s.Name.Trim(), condition.Value, s.Reason.Trim(), matches.Count,
                matches.Take(SamplesPerSuggestion).Select(u => u.AbsoluteUri).ToList()));
        }
        return result.OrderByDescending(s => s.MatchCount).ToList();
    }

    private async Task<IReadOnlyList<string>> UnmatchedUrlsAsync(Guid siteId, CancellationToken cancellationToken)
    {
        var crawlId = await db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == siteId && (c.Status == CrawlStatus.Completed || c.Status == CrawlStatus.PartiallyCompleted))
            .OrderByDescending(c => c.Number)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (crawlId is null) return [];
        return await db.PageSnapshots.AsNoTracking()
            .Where(p => p.CrawlId == crawlId && p.PageTypeId == null && p.StatusCode >= 200 && p.StatusCode < 300 && p.ContentType != null && p.ContentType.Contains("html"))
            .Select(p => p.Url)
            .ToListAsync(cancellationToken);
    }
}
