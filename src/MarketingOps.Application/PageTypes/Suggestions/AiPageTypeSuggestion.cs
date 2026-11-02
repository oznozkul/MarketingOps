namespace MarketingOps.Application.PageTypes.Suggestions;

/// <summary>Modelin önerdiği sayfa tipi. Eşleşen sayfa sayısı modelden alınmaz, desen URL'lere uygulanarak hesaplanır.</summary>
public sealed record AiPageTypeSuggestion(string Name, string UrlGlob, string Reason);
