namespace MarketingOps.Web.Controllers.Api;

public sealed record TaskTrackerRequest(string BaseUrl, string ProjectKey, string IssueType, string Email, string? ApiToken);
