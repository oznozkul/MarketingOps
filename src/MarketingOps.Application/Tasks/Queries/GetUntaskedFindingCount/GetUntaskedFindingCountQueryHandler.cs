using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tasks.Queries.GetUntaskedFindingCount;

public sealed class GetUntaskedFindingCountQueryHandler(IAppDbContext db) : IRequestHandler<GetUntaskedFindingCountQuery, int>
{
    public Task<int> Handle(GetUntaskedFindingCountQuery request, CancellationToken cancellationToken) =>
        db.Findings.AsNoTracking().CountAsync(f => f.SiteId == request.SiteId
            && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened)
            && !db.WorkTasks.Any(t => t.FindingId == f.Id), cancellationToken);
}
