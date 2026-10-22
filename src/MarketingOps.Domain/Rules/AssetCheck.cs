namespace MarketingOps.Domain.Rules;

/// <summary>Taramada örneklenen bir statik dosyanın (CSS/JS) yanıt başlıkları.</summary>
public sealed record AssetCheck(string Url, int? StatusCode, string? ContentType, string? CacheControl, string? ContentEncoding);
