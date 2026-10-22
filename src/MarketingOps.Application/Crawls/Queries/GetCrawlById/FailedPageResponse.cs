namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

public sealed record FailedPageResponse(string Url, int? StatusCode, string? FetchError);
