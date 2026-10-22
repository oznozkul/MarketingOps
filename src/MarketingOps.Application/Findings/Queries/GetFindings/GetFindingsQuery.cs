using MarketingOps.Application.Common.Models;
using MediatR;

namespace MarketingOps.Application.Findings.Queries.GetFindings;

public sealed record GetFindingsQuery(
    Guid SiteId,
    FindingFilter Filter,
    FindingSort Sort = FindingSort.Severity,
    bool Descending = false,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResponse<FindingListItemResponse>>;
