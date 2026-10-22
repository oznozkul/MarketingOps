using MarketingOps.Domain.Tracking;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;

public sealed record TagMatrixCellResponse(TagVendor Vendor, TagAuditStatus Status, IReadOnlyList<string> FoundIds);
