namespace MarketingOps.Domain.Findings;

public sealed record FindingHistoryEntry(DateTimeOffset At, FindingEventType Type, string Description, int? CrawlNumber = null, string? Actor = null);
