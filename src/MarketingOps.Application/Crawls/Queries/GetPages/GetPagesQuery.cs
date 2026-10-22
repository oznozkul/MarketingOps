using MarketingOps.Application.Common.Models;
using MediatR;

namespace MarketingOps.Application.Crawls.Queries.GetPages;

/// <summary>Bir taramadaki sayfalar. <see cref="UnmatchedOnly"/> hiçbir tipe uymayan sayfaları getirir.</summary>
public sealed record GetPagesQuery(
    Guid SiteId,
    Guid CrawlId,
    Guid? PageTypeId = null,
    bool UnmatchedOnly = false,
    PageStatusFilter? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResponse<PageListItemResponse>>;
