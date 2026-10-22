namespace MarketingOps.Domain.Findings;

/// <summary>Bir taramanın bulguya etkisi için gereken bilgiler.</summary>
public sealed record CrawlObservation(Guid CrawlId, int CrawlNumber, bool IsPartial, DateTimeOffset At);
