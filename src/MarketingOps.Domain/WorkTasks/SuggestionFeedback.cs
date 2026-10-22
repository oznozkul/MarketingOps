namespace MarketingOps.Domain.WorkTasks;

/// <summary>"Öneri faydalı mı?" geri bildirimi. Hangi öneri sürümüne verildiği <see cref="Model"/> ile tutulur.</summary>
public sealed record SuggestionFeedback(bool IsHelpful, string? Note, string Author, DateTimeOffset At, string? Model);
