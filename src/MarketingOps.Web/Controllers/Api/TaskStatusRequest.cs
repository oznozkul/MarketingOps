using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Web.Controllers.Api;

public sealed record TaskStatusRequest(WorkTaskStatus Status);
