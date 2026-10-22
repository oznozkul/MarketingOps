using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Queries.GetTaskById;

/// <summary>
/// <see cref="AiModel"/>: ayarlı AI modelinin adı (null ise AI kapalı). <see cref="HasTaskTracker"/>: site için Jira bağlı mı.
/// Geri bildirim sayıları güncel öneri sürümüne (aynı model) aittir.
/// </summary>
public sealed record TaskDetailResponse(
    Guid Id,
    string Key,
    Guid FindingId,
    string Title,
    string Problem,
    string Impact,
    TaskSuggestion Suggestion,
    IReadOnlyList<string> References,
    Team Team,
    WorkTaskStatus Status,
    string? ExternalTrackerKey,
    string? ExternalUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ClosedAt,
    IReadOnlyList<AcceptanceCriterion> Criteria,
    IReadOnlyList<TaskHistoryEntry> History,
    IReadOnlyList<TaskComment> Comments,
    int HelpfulCount,
    int NotHelpfulCount,
    string? AiModel,
    bool HasTaskTracker);
