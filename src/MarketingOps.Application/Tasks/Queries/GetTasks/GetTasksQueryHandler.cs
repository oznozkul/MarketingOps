using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Queries.GetTasks;

public sealed class GetTasksQueryHandler(IAppDbContext db) : IRequestHandler<GetTasksQuery, PagedResponse<TaskListItemResponse>>
{
    public async Task<PagedResponse<TaskListItemResponse>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var pageTypes = db.Sites.Where(s => s.Id == request.SiteId).SelectMany(s => s.PageTypes);
        var query =
            from t in db.WorkTasks.AsNoTracking()
            join f in db.Findings.AsNoTracking() on t.FindingId equals f.Id
            where t.SiteId == request.SiteId
            select new { Task = t, Finding = f };

        if (request.Statuses is { Count: > 0 } statuses) query = query.Where(x => statuses.Contains(x.Task.Status));
        if (request.Teams is { Count: > 0 } teams) query = query.Where(x => teams.Contains(x.Task.AssignedTeam));
        if (request.Severities is { Count: > 0 } severities) query = query.Where(x => severities.Contains(x.Finding.Severity));
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x => x.Task.Title.ToLower().Contains(term) || x.Task.Key.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderBy(x => x.Finding.Severity == Severity.Critical ? 0 : x.Finding.Severity == Severity.High ? 1 : x.Finding.Severity == Severity.Medium ? 2 : 3)
            .ThenByDescending(x => x.Task.UpdatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.Task.Id, x.Task.Key, x.Task.Title, x.Task.FindingId, x.Finding.Severity, FindingStatus = x.Finding.Status,
                PageTypeName = pageTypes.Where(p => p.Id == x.Finding.PageTypeId).Select(p => p.Name).FirstOrDefault(),
                x.Task.AssignedTeam, x.Task.Status, x.Task.Criteria, x.Task.ExternalTrackerKey, x.Task.ExternalUrl, x.Task.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        // Kriterler JSON kolonunda; sayım bellekte yapılır (sayfa başına en fazla 100 satır).
        var items = rows.Select(r => new TaskListItemResponse(r.Id, r.Key, r.Title, r.FindingId, r.Severity, r.FindingStatus, r.PageTypeName,
            r.AssignedTeam, r.Status, r.Criteria.Count(c => c.IsMet), r.Criteria.Count, r.ExternalTrackerKey, r.ExternalUrl, r.UpdatedAt)).ToList();
        return new PagedResponse<TaskListItemResponse>(items, request.Page, request.PageSize, total);
    }
}
