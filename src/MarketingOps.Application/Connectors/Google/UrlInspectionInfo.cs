namespace MarketingOps.Application.Connectors.Google;

public sealed record UrlInspectionInfo(string Verdict, string? CoverageState, string? IndexingState, DateTimeOffset? LastCrawlTime);
