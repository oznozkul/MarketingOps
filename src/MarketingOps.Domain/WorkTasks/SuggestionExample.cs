namespace MarketingOps.Domain.WorkTasks;

/// <summary>
/// Öneri tablosunun bir satırı: etkilenen sayfa, mevcut değer, kuralın ayrıntısı (ör. title uzunluğunda asıl title metni)
/// ve (varsa) önerilen değer.
/// </summary>
public sealed record SuggestionExample(string Url, string? Current, string? Suggested, string? Detail = null);
