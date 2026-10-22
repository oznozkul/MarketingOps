namespace MarketingOps.Domain.WorkTasks;

/// <summary>Şablondan üretilmiş görev içeriği; <see cref="WorkTask.Create"/> bunu alır.</summary>
public sealed record TaskDraft(
    string Title,
    string Problem,
    string Impact,
    TaskSuggestion Suggestion,
    IReadOnlyList<string> References,
    Team Team,
    IReadOnlyList<(string Text, VerificationType Type)> Criteria);
