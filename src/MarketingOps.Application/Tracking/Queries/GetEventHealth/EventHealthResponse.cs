namespace MarketingOps.Application.Tracking.Queries.GetEventHealth;

public sealed record EventHealthResponse(bool HasGa4, decimal DropThresholdPercent, IReadOnlyList<EventHealthRowResponse> Events);
