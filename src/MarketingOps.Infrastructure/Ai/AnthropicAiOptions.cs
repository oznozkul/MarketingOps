namespace MarketingOps.Infrastructure.Ai;

public sealed class AnthropicAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "claude-opus-5-5";

    /// <summary>Güvenlik reddinde sunucu tarafında denenecek model; modelin izin verdiği yedeklerden biri olmalı. Boşsa yedek kullanılmaz.</summary>
    public string? FallbackModel { get; set; } = "claude-opus-5";
}
