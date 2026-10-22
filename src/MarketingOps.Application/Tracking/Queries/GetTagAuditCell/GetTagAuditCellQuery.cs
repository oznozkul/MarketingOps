using MarketingOps.Domain.Tracking;
using MediatR;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditCell;

public sealed record GetTagAuditCellQuery(Guid SiteId, string PageLabel, TagVendor Vendor) : IRequest<TagAuditCellResponse>;
