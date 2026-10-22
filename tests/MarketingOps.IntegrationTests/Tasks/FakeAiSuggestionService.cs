using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.IntegrationTests.Tasks;

/// <summary>Model yerine: ilk örnek için bir öneri ve listede olmayan bir URL için uydurma bir öneri döner.</summary>
public sealed class FakeAiSuggestionService : IAiSuggestionService
{
    public string ModelName => "sahte-model";
    public string ProviderName => "Sahte";
    public bool SendsDataExternally => false;
    public List<AiSuggestionRequest> Requests { get; } = [];

    public Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        return Task.FromResult(new AiSuggestion("AI etki metni.", "AI özeti.", "AI içeriği",
        [
            new AiSuggestedValue(request.Examples[0].Url, "Önerilen başlık"),
            new AiSuggestedValue("https://uydurma.example/x", "uydurma")
        ]));
    }
}
