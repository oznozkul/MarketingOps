using MarketingOps.Domain.Tracking;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;

public sealed record TagAuditMatrixResponse(
    Guid? RunId,
    int? Number,
    AuditStatus? Status,
    DateTimeOffset? FinishedAt,
    bool ConsentTested,
    string? Error,
    string? OrderSourceError,
    IReadOnlyList<TagMatrixRowResponse> Rows);
