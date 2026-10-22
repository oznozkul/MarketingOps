namespace MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;

public sealed record TagMatrixRowResponse(string Label, Guid? PageTypeId, string Url, IReadOnlyList<TagMatrixCellResponse> Cells);
