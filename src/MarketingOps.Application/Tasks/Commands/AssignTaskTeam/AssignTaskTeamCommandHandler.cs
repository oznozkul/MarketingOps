using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AssignTaskTeam;

public sealed class AssignTaskTeamCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<AssignTaskTeamCommand>
{
    public async Task Handle(AssignTaskTeamCommand request, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        task.AssignTeam(request.Team, request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
