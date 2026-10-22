using MediatR;

namespace MarketingOps.Application.Overview.Queries.GetOverview;

public sealed record GetOverviewQuery(Guid SiteId, int Days = 30) : IRequest<OverviewResponse>;
