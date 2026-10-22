using MarketingOps.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Queries.GetTaskTrackerConnection;

public sealed class GetTaskTrackerConnectionQueryHandler(IAppDbContext db) : IRequestHandler<GetTaskTrackerConnectionQuery, TaskTrackerConnectionResponse?>
{
    public Task<TaskTrackerConnectionResponse?> Handle(GetTaskTrackerConnectionQuery request, CancellationToken cancellationToken) =>
        db.TaskTrackerConnections.AsNoTracking()
            .Where(c => c.SiteId == request.SiteId)
            .Select(c => new TaskTrackerConnectionResponse(c.BaseUrl, c.ProjectKey, c.IssueType, c.Email, c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);
}
