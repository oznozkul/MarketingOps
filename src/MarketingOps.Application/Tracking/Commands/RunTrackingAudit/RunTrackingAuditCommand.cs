using MediatR;

namespace MarketingOps.Application.Tracking.Commands.RunTrackingAudit;

public sealed record RunTrackingAuditCommand(Guid SiteId) : IRequest<Guid>;
