using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Tasks.Queries;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Web.ViewModels.Tasks;

public sealed record TaskBoardColumn(WorkTaskStatus Status, PagedResponse<TaskListItemResponse> Tasks);
