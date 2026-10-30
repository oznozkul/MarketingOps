namespace MarketingOps.Infrastructure.Ai;

/// <summary>OpenAI uyumlu <c>/chat/completions</c> uç noktası. Ollama yerelde anahtar istemez.</summary>
public sealed class OpenAiCompatibleOptions
{
    /// <summary>Arayüzde gösterilen sağlayıcı adı.</summary>
    public string Name { get; set; } = "";

    /// <summary>Model kurum içinde çalışıyor (veri dışarı çıkmıyor) mu.</summary>
    public bool IsLocal { get; set; }

    public string BaseUrl { get; set; } = "";
    public string Model { get; set; } = "";
    public string? ApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 180;

    /// <summary>Çıktı biçimi json_schema mı. LM Studio json_object kabul etmez; DeepSeek de json_schema'yı.</summary>
    public bool JsonSchema { get; set; }
}
