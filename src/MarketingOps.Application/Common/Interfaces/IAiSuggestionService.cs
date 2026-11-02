using MarketingOps.Application.PageTypes.Suggestions;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>
/// Görev önerisini zenginleştiren ve sayfa tipi öneren dil modeli. İki uygulaması var: Anthropic (Claude) ve OpenAI uyumlu API (DeepSeek, Ollama, LM Studio).
/// Hangisinin kullanılacağı yapılandırmadan seçilir; hiçbiri ayarlı değilse kayıtlı değildir: görevler şablonla kalır,
/// sayfa tipi önerisi yerine yalnızca URL grupları gösterilir.
/// </summary>
public interface IAiSuggestionService
{
    /// <summary>Arayüzde ve görev geçmişinde gösterilen model adı (ör. "claude-opus-5-5", "deepseek-chat").</summary>
    string ModelName { get; }

    /// <summary>Sağlayıcının adı (Anthropic, DeepSeek, Ollama, LM Studio).</summary>
    string ProviderName { get; }

    /// <summary>Bulgu verisi kurum dışındaki bir sunucuya gidiyor mu (yerel model değilse true).</summary>
    bool SendsDataExternally { get; }

    Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<AiPageTypeSuggestion>> SuggestPageTypesAsync(PageTypeSuggestionRequest request, CancellationToken cancellationToken);
}
