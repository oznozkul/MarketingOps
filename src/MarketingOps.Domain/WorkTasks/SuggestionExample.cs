namespace MarketingOps.Domain.WorkTasks;

/// <summary>Öneri tablosunun bir satırı: etkilenen sayfa, mevcut değer ve (varsa) önerilen değer.</summary>
public sealed record SuggestionExample(string Url, string? Current, string? Suggested);
