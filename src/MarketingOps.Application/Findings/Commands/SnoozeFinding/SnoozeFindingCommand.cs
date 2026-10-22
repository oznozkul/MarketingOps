using MediatR;

namespace MarketingOps.Application.Findings.Commands.SnoozeFinding;

public sealed record SnoozeFindingCommand(Guid SiteId, Guid FindingId, DateOnly Until, string Actor) : IRequest;
