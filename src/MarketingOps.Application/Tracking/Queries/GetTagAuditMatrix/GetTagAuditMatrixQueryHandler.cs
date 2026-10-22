using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Tracking;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditMatrix;

public sealed class GetTagAuditMatrixQueryHandler(IAppDbContext db) : IRequestHandler<GetTagAuditMatrixQuery, TagAuditMatrixResponse>
{
    public async Task<TagAuditMatrixResponse> Handle(GetTagAuditMatrixQuery request, CancellationToken cancellationToken)
    {
        var run = await db.TagAuditRuns.AsNoTracking()
            .Where(r => r.SiteId == request.SiteId)
            .OrderByDescending(r => r.Number)
            .FirstOrDefaultAsync(cancellationToken);
        if (run is null)
            return new TagAuditMatrixResponse(null, null, null, null, false, null, null, []);

        // Son deneme başarısızsa matris boş kalmasın; bir önceki tamamlanan denetimin hücreleri durur.
        var cellsFrom = run.Status == AuditStatus.Completed
            ? run
            : await db.TagAuditRuns.AsNoTracking()
                .Where(r => r.SiteId == request.SiteId && r.Status == AuditStatus.Completed)
                .OrderByDescending(r => r.Number)
                .FirstOrDefaultAsync(cancellationToken);
        var results = cellsFrom is null
            ? []
            : await db.TagAuditResults.AsNoTracking().Where(r => r.AuditRunId == cellsFrom.Id).ToListAsync(cancellationToken);
        var rows = results
            .GroupBy(r => r.Sequence)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var first = g.First();
                return new TagMatrixRowResponse(first.PageLabel, first.PageTypeId, first.Url,
                    g.OrderBy(c => c.Vendor).Select(c => new TagMatrixCellResponse(c.Vendor, c.Status, c.FoundIds)).ToList());
            })
            .ToList();
        return new TagAuditMatrixResponse(
            cellsFrom?.Id ?? run.Id,
            run.Number,
            run.Status,
            run.Status == AuditStatus.Running ? null : run.FinishedAt,
            cellsFrom?.ConsentTested ?? false,
            run.Error,
            run.OrderSourceError ?? cellsFrom?.OrderSourceError,
            rows);
    }
}
