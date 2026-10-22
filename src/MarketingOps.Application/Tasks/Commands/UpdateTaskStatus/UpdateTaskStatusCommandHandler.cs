using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.UpdateTaskStatus;

public sealed class UpdateTaskStatusCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<UpdateTaskStatusCommand>
{
    public async Task Handle(UpdateTaskStatusCommand request, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        task.ChangeStatus(request.Status, request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
