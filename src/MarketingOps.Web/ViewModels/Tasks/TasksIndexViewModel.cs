using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Tasks.Queries;

namespace MarketingOps.Web.ViewModels.Tasks;

/// <summary>Tablo görünümünde <see cref="Table"/>, pano görünümünde <see cref="Columns"/> doludur.</summary>
public sealed record TasksIndexViewModel(
    TasksQuery Query,
    PagedResponse<TaskListItemResponse>? Table,
    IReadOnlyList<TaskBoardColumn> Columns,
    int UntaskedFindingCount,
    IReadOnlyList<FilterMenuViewModel> Menus,
    IReadOnlyList<ChipViewModel> Chips);
