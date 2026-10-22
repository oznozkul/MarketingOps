namespace MarketingOps.Domain.Findings;

/// <summary>Bulgudan etkilenen bir sayfa: URL, kuralın gördüğü mevcut değer ve açıklama.</summary>
public sealed record FindingOccurrence(string Url, string? CurrentValue, string? Detail, Guid LastSeenCrawlId);
