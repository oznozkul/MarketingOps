namespace MarketingOps.Domain.Findings;

/// <summary>Bulgudan etkilenen bir sayfa: URL, kuralın gördüğü mevcut değer, açıklama ve (render bulgularında) cihazlar.</summary>
public sealed record FindingOccurrence(string Url, string? CurrentValue, string? Detail, Guid LastSeenCrawlId, string? Devices = null);
