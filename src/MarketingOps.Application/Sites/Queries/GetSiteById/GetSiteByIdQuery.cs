using MediatR;

namespace MarketingOps.Application.Sites.Queries.GetSiteById;

public sealed record GetSiteByIdQuery(Guid SiteId) : IRequest<SiteDetailResponse>;
