namespace MarketingOps.Domain.WorkTasks;

public sealed record TaskHistoryEntry(DateTimeOffset At, TaskEventType Type, string Description, string? Actor = null);
