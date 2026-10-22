using MarketingOps.Application.Common.Exceptions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Tracking;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Tracking.Queries.GetTagAuditCell;

public sealed class GetTagAuditCellQueryHandler(IAppDbContext db) : IRequestHandler<GetTagAuditCellQuery, TagAuditCellResponse>
{
    public async Task<TagAuditCellResponse> Handle(GetTagAuditCellQuery request, CancellationToken cancellationToken)
    {
        var runId = await db.TagAuditRuns.AsNoTracking()
            .Where(r => r.SiteId == request.SiteId && r.Status == AuditStatus.Completed)
            .OrderByDescending(r => r.Number)
            .Select(r => (Guid?)r.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("Tamamlanmış denetim yok.");

        var cell = await db.TagAuditResults.AsNoTracking()
            .FirstOrDefaultAsync(r => r.AuditRunId == runId && r.PageLabel == request.PageLabel && r.Vendor == request.Vendor, cancellationToken)
            ?? throw new NotFoundException("Denetim hücresi bulunamadı.");

        var site = await db.Sites.AsNoTracking().FirstAsync(s => s.Id == request.SiteId, cancellationToken);
        var ruleCode = RuleCode(cell.Status);
        Guid? findingId = null;
        string? taskKey = null;
        if (ruleCode is not null)
        {
            var fingerprint = Finding.ComputeFingerprint(request.SiteId, ruleCode, cell.PageTypeId);
            findingId = await db.Findings.AsNoTracking()
                .Where(f => f.Fingerprint == fingerprint && f.Status != FindingStatus.Resolved && f.Status != FindingStatus.FalsePositive)
                .Select(f => (Guid?)f.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (findingId is Guid id)
                taskKey = await db.WorkTasks.AsNoTracking().Where(t => t.FindingId == id).Select(t => t.Key).FirstOrDefaultAsync(cancellationToken);
        }

        return new TagAuditCellResponse(
            cell.PageLabel, cell.Url, cell.Vendor, cell.Status,
            TagAuditBuilder.Expected(site.ExpectedTrackingIds, cell.Vendor),
            cell.FoundIds, cell.Requests, findingId, taskKey);
    }

    private static string? RuleCode(TagAuditStatus status) => status switch
    {
        TagAuditStatus.Missing => "track.tag.missing",
        TagAuditStatus.Duplicate => "track.tag.duplicate",
        TagAuditStatus.WrongId => "track.tag.wrong_id",
        TagAuditStatus.FiresBeforeConsent => "track.consent.prefire",
        _ => null
    };
}
