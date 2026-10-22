using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Findings.Queries.GetFindingSummary;

namespace MarketingOps.Web.ViewModels.Findings;

public sealed record FindingsIndexViewModel(
    FindingsQuery Query,
    FindingSummaryResponse Summary,
    PagedResponse<FindingListItemResponse> Findings,
    IReadOnlyList<FilterMenuViewModel> Menus,
    IReadOnlyList<ChipViewModel> Chips);
