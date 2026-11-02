using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.PageTypes.Suggestions;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// Claude ile öneri. Yapılandırılmış çıktı (JSON şeması) kullanılır. Güvenlik sınıflandırıcısı isteği reddederse
/// sunucu tarafı yedek model devreye girer; yine reddedilirse çağıran şablona (görev) ya da URL gruplarına (sayfa tipi) düşer.
/// </summary>
public sealed class AnthropicSuggestionService(AnthropicClient client, AnthropicAiOptions options, KnowledgeBase knowledge) : IAiSuggestionService
{

    public string ModelName => options.Model;
    public string ProviderName => "Anthropic";
    public bool SendsDataExternally => true;

    public async Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken) =>
        SuggestionPrompt.Parse(await CompleteAsync(SuggestionPrompt.System, SuggestionPrompt.Schema, SuggestionPrompt.User(request, knowledge.For(request.ReferenceKey)), cancellationToken), request);

    public async Task<IReadOnlyList<AiPageTypeSuggestion>> SuggestPageTypesAsync(PageTypeSuggestionRequest request, CancellationToken cancellationToken) =>
        PageTypePrompt.Parse(await CompleteAsync(PageTypePrompt.System, PageTypePrompt.Schema, PageTypePrompt.User(request), cancellationToken));

    private async Task<string> CompleteAsync(string system, JsonElement schema, string user, CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = options.Model,
            MaxTokens = 16000,
            System = system,
            OutputConfig = new BetaOutputConfig
            {
                Effort = Effort.Medium,
                Format = new BetaJsonOutputFormat { Schema = schema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()) }
            },
            Messages = [new() { Role = Role.User, Content = user }]
        };
        if (!string.IsNullOrWhiteSpace(options.FallbackModel))
            parameters = parameters with
            {
                Betas = ["server-side-fallback-2026-06-01"],
                Fallbacks = new List<BetaFallbackParam> { new() { Model = options.FallbackModel } }
            };
        var response = await client.Beta.Messages.Create(parameters, cancellationToken);

        if (response.StopReason == "refusal") throw new InvalidOperationException("Model isteği reddetti.");
        if (response.StopReason == "max_tokens") throw new InvalidOperationException("Model yanıtı yarıda kesildi.");
        return string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
    }
}
