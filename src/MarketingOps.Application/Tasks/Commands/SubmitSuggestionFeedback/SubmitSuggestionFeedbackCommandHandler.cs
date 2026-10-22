using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Application.Tasks.Commands.SubmitSuggestionFeedback;

public sealed class SubmitSuggestionFeedbackCommandHandler(IAppDbContext db, TimeProvider clock) : IRequestHandler<SubmitSuggestionFeedbackCommand>
{
    public async Task Handle(SubmitSuggestionFeedbackCommand request, CancellationToken cancellationToken)
    {
        var task = await db.WorkTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        task.AddFeedback(request.IsHelpful, request.Note, request.Author, clock.GetUtcNow());
        await db.SaveChangesAsync(cancellationToken);
    }
}
