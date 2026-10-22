using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Queries.GetCrawlById;
using MarketingOps.Application.Crawls.Queries.GetPages;
using MarketingOps.Application.Findings.Queries.GetCrawlFindingChanges;

namespace MarketingOps.Web.ViewModels.Crawls;

public sealed record CrawlDetailViewModel(
    CrawlDetailResponse Detail,
    PagedResponse<PageListItemResponse> Pages,
    PagesFilter Filter,
    IReadOnlyList<FindingChangeResponse> FindingChanges);
