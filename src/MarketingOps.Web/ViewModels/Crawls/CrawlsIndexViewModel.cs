using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Queries;
using MarketingOps.Application.Crawls.Queries.GetCrawlStatus;

namespace MarketingOps.Web.ViewModels.Crawls;

public sealed record CrawlsIndexViewModel(CrawlStatusResponse Status, PagedResponse<CrawlListItemResponse> Crawls);
