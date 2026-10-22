using MarketingOps.Application.Findings.Queries.GetFindingById;
using MarketingOps.Application.Metrics.Queries.GetSearchPerformance;
using MarketingOps.Application.Tasks.Queries.GetTaskById;

namespace MarketingOps.Web.ViewModels.Findings;

/// <summary><see cref="Task"/> null ise bulgunun henüz görevi yok. <see cref="Search"/> null ise Search Console verisi yok.</summary>
public sealed record FindingDetailViewModel(FindingDetailResponse Detail, string? OccurrenceSearch, TaskDetailResponse? Task, SearchPerformanceResponse? Search = null);
