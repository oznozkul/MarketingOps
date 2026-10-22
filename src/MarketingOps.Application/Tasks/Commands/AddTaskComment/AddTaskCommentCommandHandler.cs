using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.AddTaskComment;

public sealed class AddTaskCommentCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<AddTaskCommentCommand>
{
    public async Task Handle(AddTaskCommentCommand request, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        task.AddComment(request.Author, request.Text, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
