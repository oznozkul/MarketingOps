using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Messages;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Tasks.Suggestions;

namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// Claude ile öneri. Yapılandırılmış çıktı (JSON şeması) kullanılır. Güvenlik sınıflandırıcısı isteği reddederse
/// sunucu tarafı yedek model devreye girer; yine reddedilirse görev şablon önerisiyle kalır.
/// </summary>
public sealed class AnthropicSuggestionService(AnthropicClient client, AnthropicAiOptions options, KnowledgeBase knowledge) : IAiSuggestionService
{

    public string ModelName => options.Model;
    public string ProviderName => "Anthropic";
    public bool SendsDataExternally => true;

    public async Task<AiSuggestion> SuggestAsync(AiSuggestionRequest request, CancellationToken cancellationToken)
    {
        var parameters = new MessageCreateParams
        {
            Model = options.Model,
            MaxTokens = 16000,
            System = SuggestionPrompt.System,
            OutputConfig = new BetaOutputConfig
            {
                Effort = Effort.Medium,
                Format = new BetaJsonOutputFormat { Schema = SuggestionPrompt.Schema.EnumerateObject().ToDictionary(p => p.Name, p => p.Value.Clone()) }
            },
            Messages = [new() { Role = Role.User, Content = SuggestionPrompt.User(request, knowledge.For(request.ReferenceKey)) }]
        };
        if (!string.IsNullOrWhiteSpace(options.FallbackModel))
            parameters = parameters with
            {
                Betas = ["server-side-fallback-2026-06-01"],
                Fallbacks = new List<BetaFallbackParam> { new() { Model = options.FallbackModel } }
            };
        var response = await client.Beta.Messages.Create(parameters, cancellationToken);

        if (response.StopReason == "refusal") throw new InvalidOperationException("Model isteği reddetti; görev şablon önerisiyle kaldı.");
        if (response.StopReason == "max_tokens") throw new InvalidOperationException("Model yanıtı yarıda kesildi.");
        var json = string.Concat(response.Content.Select(b => b.Value).OfType<BetaTextBlock>().Select(t => t.Text));
        return SuggestionPrompt.Parse(json, request);
    }
}
