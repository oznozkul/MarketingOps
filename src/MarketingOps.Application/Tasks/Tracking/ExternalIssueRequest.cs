namespace MarketingOps.Application.Tasks.Tracking;

public sealed record ExternalIssueRequest(string? ExistingKey, string Title, string Description, IReadOnlyList<string> Labels);
