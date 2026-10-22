using MarketingOps.Domain.Tracking;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditCell;

public sealed record TagAuditCellResponse(
    string Label,
    string Url,
    TagVendor Vendor,
    TagAuditStatus Status,
    string? ExpectedId,
    IReadOnlyList<string> FoundIds,
    IReadOnlyList<CapturedTagRequest> Requests,
    Guid? FindingId,
    string? TaskKey);
