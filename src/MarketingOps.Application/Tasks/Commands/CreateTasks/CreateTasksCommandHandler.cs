using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Commands.CreateTasks;

public sealed class CreateTasksCommandHandler(IAppDbContext db, WorkTaskFactory factory, ITaskJobScheduler jobs) : IRequestHandler<CreateTasksCommand, int>
{
    public async Task<int> Handle(CreateTasksCommand request, CancellationToken cancellationToken)
    {
        var query = db.Findings.Where(f => f.SiteId == request.SiteId && !db.WorkTasks.Any(t => t.FindingId == f.Id));
        query = request.FindingIds is { Count: > 0 } ids
            ? query.Where(f => ids.Contains(f.Id))
            : query.Where(f => f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened);
        var findings = await query.OrderBy(f => f.FirstSeenAt).ToListAsync(cancellationToken);

        var created = new List<Domain.WorkTasks.WorkTask>();
        foreach (var finding in findings)
            if (await factory.CreateAsync(finding, cancellationToken) is { } task) created.Add(task);

        if (created.Count == 0) return 0;
        await db.SaveChangesAsync(cancellationToken);
        foreach (var task in created) jobs.EnqueueEnrichment(task.Id);
        return created.Count;
    }
}
