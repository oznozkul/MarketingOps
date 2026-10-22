namespace MarketingOps.Infrastructure.Ai;

/// <summary>
/// "Ai" yapılandırma bölümü. <see cref="Provider"/>: None (varsayılan), Anthropic, DeepSeek ya da Ollama.
/// DeepSeek ve Ollama OpenAI uyumlu sohbet API'si kullanır; yalnızca adres, model ve anahtar farklıdır.
/// </summary>
public sealed class AiOptions
{
    public const string Section = "Ai";

    public AiProvider Provider { get; set; } = AiProvider.None;
    public AnthropicAiOptions Anthropic { get; set; } = new();
    public OpenAiCompatibleOptions DeepSeek { get; set; } = new() { Name = "DeepSeek", BaseUrl = "https://api.deepseek.com", Model = "deepseek-chat" };
    public OpenAiCompatibleOptions Ollama { get; set; } = new() { Name = "Ollama (yerel)", IsLocal = true, BaseUrl = "http://localhost:11434/v1", Model = "qwen2.5:32b" };
}
