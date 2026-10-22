namespace MarketingOps.Application.Crawls.Queries.GetPages;

public sealed record PageListItemResponse(
    Guid Id,
    string Url,
    string? PageTypeName,
    int? StatusCode,
    int RedirectCount,
    int? ResponseTimeMs,
    int? HtmlSizeBytes,
    string? Title,
    string? FetchError);
