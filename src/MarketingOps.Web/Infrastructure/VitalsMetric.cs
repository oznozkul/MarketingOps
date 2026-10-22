namespace MarketingOps.Web.Infrastructure;

/// <summary>Bir Core Web Vitals metriği ve Google eşikleri: <see cref="Good"/> altı iyi, <see cref="Poor"/> üstü zayıf.</summary>
public sealed record VitalsMetric(string Name, double Good, double Poor, string Unit);
