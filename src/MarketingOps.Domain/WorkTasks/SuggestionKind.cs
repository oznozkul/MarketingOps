namespace MarketingOps.Domain.WorkTasks;

/// <summary>Önerinin biçimi: metin kalıbı (title/meta), kod bloğu (JSON-LD, etiket) ya da adım listesi (sunucu/GTM ayarı).</summary>
public enum SuggestionKind
{
    Text,
    Code,
    Steps
}
