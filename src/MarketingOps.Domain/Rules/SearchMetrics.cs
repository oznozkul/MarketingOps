namespace MarketingOps.Domain.Rules;

/// <summary>Bir sayfanın son 28 günlük arama performansı (Search Console).</summary>
public sealed record SearchMetrics(int Clicks, int Impressions, double AveragePosition);
