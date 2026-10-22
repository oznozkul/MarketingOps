using MediatR;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AssignTaskTeam;

public sealed record AssignTaskTeamCommand(Guid SiteId, Guid TaskId, Team Team, string Actor) : IRequest;
