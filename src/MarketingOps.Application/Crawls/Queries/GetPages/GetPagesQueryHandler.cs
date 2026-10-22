using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Crawls.Queries.GetPages;

public sealed class GetPagesQueryHandler(IAppDbContext db) : IRequestHandler<GetPagesQuery, PagedResponse<PageListItemResponse>>
{
    public async Task<PagedResponse<PageListItemResponse>> Handle(GetPagesQuery request, CancellationToken cancellationToken)
    {
        if (!await db.Crawls.AnyAsync(c => c.Id == request.CrawlId && c.SiteId == request.SiteId, cancellationToken))
            throw new NotFoundException("Tarama bulunamadı.");

        var pages = db.PageSnapshots.AsNoTracking().Where(p => p.CrawlId == request.CrawlId);
        if (request.UnmatchedOnly) pages = pages.Where(p => p.PageTypeId == null);
        else if (request.PageTypeId is { } pageTypeId) pages = pages.Where(p => p.PageTypeId == pageTypeId);

        pages = request.Status switch
        {
            PageStatusFilter.Success => pages.Where(p => p.StatusCode >= 200 && p.StatusCode < 300),
            PageStatusFilter.Redirect => pages.Where(p => p.StatusCode >= 300 && p.StatusCode < 400),
            PageStatusFilter.ClientError => pages.Where(p => p.StatusCode >= 400 && p.StatusCode < 500),
            PageStatusFilter.ServerError => pages.Where(p => p.StatusCode >= 500),
            PageStatusFilter.NoResponse => pages.Where(p => p.StatusCode == null),
            _ => pages
        };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            pages = pages.Where(p => p.Url.ToLower().Contains(term));
        }

        var pageTypes = db.Sites.Where(s => s.Id == request.SiteId).SelectMany(s => s.PageTypes);
        var total = await pages.CountAsync(cancellationToken);
        var rows = await pages
            .OrderBy(p => p.Url)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                p.Id,
                p.Url,
                PageTypeName = pageTypes.Where(t => t.Id == p.PageTypeId).Select(t => t.Name).FirstOrDefault(),
                p.StatusCode,
                p.RedirectChain,
                p.ResponseTimeMs,
                p.HtmlSizeBytes,
                p.Data,
                p.FetchError
            })
            .ToListAsync(cancellationToken);

        // Snapshot verisi tek bir JSON değeri olarak saklandığından başlık ve yönlendirme sayısı bellekte okunur.
        var items = rows.Select(p => new PageListItemResponse(p.Id, p.Url, p.PageTypeName, p.StatusCode, p.RedirectChain.Count,
            p.ResponseTimeMs, p.HtmlSizeBytes, p.Data.Title, p.FetchError)).ToList();

        return new PagedResponse<PageListItemResponse>(items, request.Page, request.PageSize, total);
    }
}
