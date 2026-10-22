using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetTrackingScore;

public sealed record GetTrackingScoreQuery(Guid SiteId) : IRequest<TrackingScoreResponse>;
