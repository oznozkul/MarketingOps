namespace MarketingOps.Domain.WorkTasks;

public sealed record TaskComment(Guid Id, string Author, string Text, DateTimeOffset At);
