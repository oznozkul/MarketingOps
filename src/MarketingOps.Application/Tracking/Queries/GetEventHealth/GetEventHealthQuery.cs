using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetEventHealth;

public sealed record GetEventHealthQuery(Guid SiteId) : IRequest<EventHealthResponse>;
