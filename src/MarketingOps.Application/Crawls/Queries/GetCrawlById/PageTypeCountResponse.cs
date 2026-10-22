namespace MarketingOps.Application.Crawls.Queries.GetCrawlById;

/// <summary><see cref="PageTypeId"/> null ise hiçbir tipe uymayan sayfalar.</summary>
public sealed record PageTypeCountResponse(Guid? PageTypeId, string? Name, int Count);
