namespace MarketingOps.Application.Metrics.Queries.GetWebVitals;

/// <summary><see cref="MeasuredOn"/> null ise hiç ölçüm yok.</summary>
public sealed record WebVitalsResponse(DateOnly? MeasuredOn, IReadOnlyList<PageTypeVitalsResponse> PageTypes);
