using MediatR;

namespace MarketingOps.Application.Tasks.Queries.GetTaskById;

public sealed record GetTaskByIdQuery(Guid SiteId, Guid TaskId) : IRequest<TaskDetailResponse>;
