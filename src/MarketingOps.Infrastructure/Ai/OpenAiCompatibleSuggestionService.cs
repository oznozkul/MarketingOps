using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// OpenAI uyumlu <c>/chat/completions</c> (DeepSeek, Ollama). İki sağlayıcının ortak paydası olan <c>json_object</c> çıktı biçimi
/// kullanılır; şema istemde tarif edilir ve yanıt her durumda doğrulanır.
/// </summary>
public sealed class OpenAiCompatibleSuggestionService(HttpClient http, OpenAiCompatibleOptions options, KnowledgeBase knowledge) : IAiSuggestionService
{
    public string ModelName => options.Model;
    public string ProviderName => options.Name;
    public bool SendsDataExternally => !options.IsLocal;

    public async Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["temperature"] = 0.2,
            ["response_format"] = new JsonObject { ["type"] = "json_object" },
            ["messages"] = new JsonArray
            {
                new JsonObject { ["role"] = "system", ["content"] = SuggestionPrompt.System + "\nYanıt biçimi (JSON): " + SuggestionPrompt.Schema.GetRawText() },
                new JsonObject { ["role"] = "user", ["content"] = SuggestionPrompt.User(request, knowledge.For(request.ReferenceKey)) }
            }
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, options.BaseUrl.TrimEnd('/') + "/chat/completions") { Content = JsonContent.Create(body) };
        if (!string.IsNullOrWhiteSpace(options.ApiKey)) message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var response = await http.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"{options.Model} yanıtı başarısız: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync(cancellationToken)}");

        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        var choice = json.RootElement.GetProperty("choices")[0];
        if (choice.TryGetProperty("finish_reason", out var reason) && reason.GetString() == "length")
            throw new InvalidOperationException("Model yanıtı yarıda kesildi.");
        var content = choice.GetProperty("message").GetProperty("content").GetString() ?? "";
        return SuggestionPrompt.Parse(StripFence(content), request);
    }

    /// <summary>Bazı yerel modeller JSON'u ```json bloğuna sarar.</summary>
    private static string StripFence(string text)
    {
        var t = text.Trim();
        if (!t.StartsWith("```", StringComparison.Ordinal)) return t;
        var start = t.IndexOf('\n');
        var end = t.LastIndexOf("```", StringComparison.Ordinal);
        return start >= 0 && end > start ? t[(start + 1)..end].Trim() : t;
    }
}
