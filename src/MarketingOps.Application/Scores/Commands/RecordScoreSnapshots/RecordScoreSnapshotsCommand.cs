using MediatR;

namespace MarketingOps.Application.Scores.Commands.RecordScoreSnapshots;

public sealed record RecordScoreSnapshotsCommand(Guid SiteId) : IRequest;
