using MarketingOps.Application.Common.Models;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;
using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetTasks;

/// <summary>Görev listesi; boş filtre listesi "hepsi" demektir. Varsayılan sıra: önem, sonra son güncelleme.</summary>
public sealed record GetTasksQuery(
    Guid SiteId,
    IReadOnlyList<WorkTaskStatus>? Statuses = null,
    IReadOnlyList<Team>? Teams = null,
    IReadOnlyList<Severity>? Severities = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResponse<TaskListItemResponse>>;
