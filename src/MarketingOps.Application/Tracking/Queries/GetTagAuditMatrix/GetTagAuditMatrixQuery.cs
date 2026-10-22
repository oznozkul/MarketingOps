using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;

public sealed record GetTagAuditMatrixQuery(Guid SiteId) : IRequest<TagAuditMatrixResponse>;
