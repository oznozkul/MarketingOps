namespace MarketingOps.Web.Controllers.Api;

public sealed record CreateTasksRequest(IReadOnlyList<Guid>? FindingIds);
