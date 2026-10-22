namespace MarketingOps.Application.Metrics.Queries.GetSearchPerformance;

/// <summary><see cref="AveragePosition"/> gösterimle ağırlıklı ortalamadır. <see cref="PageCount"/>: verisi olan sayfa sayısı.</summary>
public sealed record SearchPerformanceResponse(DateOnly From, DateOnly To, int PageCount, int Clicks, int Impressions, double Ctr, double? AveragePosition);
