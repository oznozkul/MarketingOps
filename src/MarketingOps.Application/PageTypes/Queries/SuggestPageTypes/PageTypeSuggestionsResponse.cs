using MarketingOps.Application.PageTypes.Suggestions;

namespace MarketingOps.Application.PageTypes.Queries.SuggestPageTypes;

/// <param name="AiAvailable">AI sağlayıcısı yapılandırılmış mı; değilse yalnızca URL grupları döner.</param>
/// <param name="Error">AI çağrısı başarısız olduysa kullanıcıya gösterilecek açıklama; gruplar yine döner.</param>
public sealed record PageTypeSuggestionsResponse(
    PageTypeSuggestionSource Source,
    bool AiAvailable,
    int UrlCount,
    IReadOnlyList<UrlGroup> Groups,
    IReadOnlyList<PageTypeSuggestionResponse> Suggestions,
    string? Error);
