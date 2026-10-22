namespace MarketingOps.Web.Controllers.Api;

public sealed record ExportTasksRequest(IReadOnlyList<Guid> TaskIds);
