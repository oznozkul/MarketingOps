namespace MarketingOps.Application.Tracking.Queries.GetEventHealth;

public sealed record EventHealthRowResponse(string Name, long Yesterday, decimal Average, decimal? ChangePercent, bool Anomaly, IReadOnlyList<EventPointResponse> Series);
