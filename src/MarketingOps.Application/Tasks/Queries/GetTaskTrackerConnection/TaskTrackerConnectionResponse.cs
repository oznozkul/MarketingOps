namespace MarketingOps.Application.Tasks.Queries.GetTaskTrackerConnection;

public sealed record TaskTrackerConnectionResponse(string BaseUrl, string ProjectKey, string IssueType, string Email, DateTimeOffset UpdatedAt);
