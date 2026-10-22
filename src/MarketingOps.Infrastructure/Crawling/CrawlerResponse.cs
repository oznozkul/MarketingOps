namespace MarketingOps.Infrastructure.Crawling;

/// <summary>Tek bir HTTP isteğinin sonucu (yönlendirme izlenmez). <see cref="Body"/> yalnızca metin içerik için doludur.</summary>
public sealed record CrawlerResponse(
    int StatusCode,
    Uri? Location,
    string? ContentType,
    string? ContentEncoding,
    string? CacheControl,
    string? Body,
    int? BodySizeBytes,
    int ResponseTimeMs);
