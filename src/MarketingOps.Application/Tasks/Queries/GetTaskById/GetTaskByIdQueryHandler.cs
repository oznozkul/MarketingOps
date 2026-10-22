using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Queries.GetTaskById;

public sealed class GetTaskByIdQueryHandler(IAppDbContext db, IAiSuggestionService? ai = null) : IRequestHandler<GetTaskByIdQuery, TaskDetailResponse>
{
    public async Task<TaskDetailResponse> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await db.WorkTasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TaskId && t.SiteId == request.SiteId, cancellationToken)
            ?? throw new NotFoundException("Görev bulunamadı.");
        var feedback = t.Feedback.Where(f => f.Model == t.Suggestion.Model).ToList();

        return new TaskDetailResponse(t.Id, t.Key, t.FindingId, t.Title, t.Problem, t.Impact, t.Suggestion, t.References, t.AssignedTeam, t.Status,
            t.CreatedAt, t.ClosedAt, t.Criteria, t.History.OrderByDescending(h => h.At).ToList(),
            t.Comments.OrderBy(c => c.At).ToList(), feedback.Count(f => f.IsHelpful), feedback.Count(f => !f.IsHelpful), ai?.ModelName);
    }
}
