using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetOrderComparison;

public sealed record GetOrderComparisonQuery(Guid SiteId) : IRequest<OrderComparisonResponse>;
