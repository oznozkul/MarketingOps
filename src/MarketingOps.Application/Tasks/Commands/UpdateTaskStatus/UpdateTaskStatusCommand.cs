using MediatR;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.UpdateTaskStatus;

public sealed record UpdateTaskStatusCommand(Guid SiteId, Guid TaskId, WorkTaskStatus Status, string Actor) : IRequest;
