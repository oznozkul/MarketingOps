using MediatR;

namespace MarketingOps.Application.Findings.Queries.ExportFindings;

/// <summary>Filtredeki bulguların tamamı (CSV indirme için), üst sınırla.</summary>
public sealed record ExportFindingsQuery(Guid SiteId, FindingFilter Filter, FindingSort Sort = FindingSort.Severity, bool Descending = false)
    : IRequest<IReadOnlyList<FindingListItemResponse>>;
