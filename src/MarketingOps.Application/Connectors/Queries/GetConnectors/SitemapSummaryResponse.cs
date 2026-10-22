namespace MarketingOps.Application.Connectors.Queries.GetConnectors;

public sealed record SitemapSummaryResponse(int Count, int Errors, int Warnings, long? Submitted, long? Indexed, DateTimeOffset SyncedAt);
