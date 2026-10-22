using MarketingOps.Application.Common.Models;
using MediatR;

namespace MarketingOps.Application.Sites.Queries.GetSites;

public sealed record GetSitesQuery(int Page = 1, int PageSize = 25) : IRequest<PagedResponse<SiteListItemResponse>>;
