using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.ConfirmAcceptanceCriterion;

public sealed class ConfirmAcceptanceCriterionCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<ConfirmAcceptanceCriterionCommand>
{
    public async Task Handle(ConfirmAcceptanceCriterionCommand request, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        if (request.Confirmed) task.ConfirmCriterion(request.CriterionId, request.Actor, clock.GetUtcNow());
        else task.RevertCriterion(request.CriterionId, request.Actor, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
