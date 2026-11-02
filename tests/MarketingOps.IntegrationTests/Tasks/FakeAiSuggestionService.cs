using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.PageTypes.Suggestions;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.IntegrationTests.Tasks;

/// <summary>
/// Model yerine. Görev: ilk örnek için bir öneri ve listede olmayan bir URL için uydurma bir öneri.
/// Sayfa tipi: sahte sitenin ürün ve kategori desenleri, hiçbir sayfaya uymayan bir desen ve geçersiz bir desen.
/// </summary>
public sealed class FakeAiSuggestionService : IAiSuggestionService
{
    public string ModelName => "sahte-model";
    public string ProviderName => "Sahte";
    public bool SendsDataExternally => false;
    public List<AiSuggestionRequest> Requests { get; } = [];
    public List<PageTypeSuggestionRequest> PageTypeRequests { get; } = [];

    public Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new AiSuggestion("AI etki metni.", "AI özeti.", "AI içeriği",
        [
            new AiSuggestedValue(request.Examples[0].Url, "Önerilen başlık"),
            new AiSuggestedValue("https://uydurma.example/x", "uydurma")
        ]));
    }

    public Task<IReadOnlyList<AiPageTypeSuggestion>> SuggestPageTypesAsync(PageTypeSuggestionRequest request, CancellationToken cancellationToken)
    {
        PageTypeRequests.Add(request);
        return Task.FromResult<IReadOnlyList<AiPageTypeSuggestion>>(
        [
            new AiPageTypeSuggestion("Ürün", "/urun/*", "Ürün klasöründeki sayfalar."),
            new AiPageTypeSuggestion("Kategori", "/kategori/**", "Kategori klasörü."),
            new AiPageTypeSuggestion("Hayalet", "/yok/**", "Hiçbir sayfaya uymaz."),
            new AiPageTypeSuggestion("Bozuk", "   ", "Boş desen.")
        ]);
    }
}
