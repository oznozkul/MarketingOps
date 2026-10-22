namespace MarketingOps.Domain.Rules;

/// <summary>Taramada örneklenen bir statik dosyanın (CSS/JS) yanıtı. Süre ve boyut eski taramalarda yoktur.</summary>
public sealed record AssetCheck(
    string Url,
    int? StatusCode,
    string? ContentType,
    string? CacheControl,
    string? ContentEncoding,
    int? ResponseTimeMs = null,
    int? SizeBytes = null);
