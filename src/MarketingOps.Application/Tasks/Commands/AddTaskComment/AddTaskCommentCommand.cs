using MediatR;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AddTaskComment;

public sealed record AddTaskCommentCommand(Guid SiteId, Guid TaskId, string Text, string Author) : IRequest;
