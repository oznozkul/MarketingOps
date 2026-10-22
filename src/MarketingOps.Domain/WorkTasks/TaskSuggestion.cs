namespace MarketingOps.Domain.WorkTasks;

/// <summary>
/// Görevdeki önerilen çözüm. <see cref="Model"/> null ise öneri kural şablonundandır; doluysa o modelle üretilmiştir
/// ve arayüzde "AI tarafından üretildi" notuyla gösterilir.
/// </summary>
public sealed record TaskSuggestion(SuggestionKind Kind, string Summary, string Content, IReadOnlyList<SuggestionExample> Examples, string? Model = null);
