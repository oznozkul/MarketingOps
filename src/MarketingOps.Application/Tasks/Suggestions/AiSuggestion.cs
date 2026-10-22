namespace MarketingOps.Application.Tasks.Suggestions;

/// <summary>Modelin yapılandırılmış çıktısı. <see cref="SuggestedValues"/> yalnızca metin önerilerinde (title/meta) dolar.</summary>
public sealed record AiSuggestion(string Impact, string Summary, string Content, IReadOnlyList<AiSuggestedValue> SuggestedValues);
