using MediatR;

namespace MarketingOps.Application.Findings.Commands.RevertFalsePositive;

public sealed record RevertFalsePositiveCommand(Guid SiteId, Guid FindingId, string Actor) : IRequest;
