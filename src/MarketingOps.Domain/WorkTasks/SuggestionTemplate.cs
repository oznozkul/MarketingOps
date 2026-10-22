namespace MarketingOps.Domain.WorkTasks;

/// <summary>Kuralın AI'sız önerisi. Sayı içermez; somut değerler görev oluşturulurken bulgudan eklenir.</summary>
public sealed record SuggestionTemplate(SuggestionKind Kind, string Summary, string Content);
