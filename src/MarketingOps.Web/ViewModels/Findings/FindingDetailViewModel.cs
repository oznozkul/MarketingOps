using MarketingOps.Application.Findings.Queries.GetFindingById;
using MarketingOps.Application.Tasks.Queries.GetTaskById;

namespace MarketingOps.Web.ViewModels.Findings;

/// <summary><see cref="Task"/> null ise bulgunun henüz görevi yok.</summary>
public sealed record FindingDetailViewModel(FindingDetailResponse Detail, string? OccurrenceSearch, TaskDetailResponse? Task);
