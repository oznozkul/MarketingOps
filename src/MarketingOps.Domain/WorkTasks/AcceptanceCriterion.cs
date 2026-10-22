namespace MarketingOps.Domain.WorkTasks;

public sealed record AcceptanceCriterion(Guid Id, string Text, VerificationType VerificationType, bool IsMet = false, DateTimeOffset? MetAt = null, string? MetBy = null);
