using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Findings;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries;

public static class FindingProjection
{
    /// <summary>Liste satırına projeksiyon; sayfa tipi adı alt sorguyla okunur.</summary>
    public static IQueryable<FindingListItemResponse> ToListItems(this IQueryable<Finding> findings, IAppDbContext db, Guid siteId, Guid? latestCrawlId)
    {
        var pageTypes = db.Sites.Where(s => s.Id == siteId).SelectMany(s => s.PageTypes);
        return findings.Select(f => new FindingListItemResponse(
            f.Id, f.Title, f.RuleCode, f.Category, f.Severity, f.Status, f.PageTypeId,
            pageTypes.Where(p => p.Id == f.PageTypeId).Select(p => p.Name).FirstOrDefault(),
            f.AffectedCount, f.FirstSeenAt, f.LastSeenAt,
            latestCrawlId != null && f.LastOpenedCrawlId == latestCrawlId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened),
            db.WorkTasks.Where(t => t.FindingId == f.Id).Select(t => (Guid?)t.Id).FirstOrDefault(),
            db.WorkTasks.Where(t => t.FindingId == f.Id).Select(t => t.Key).FirstOrDefault()));
    }
}
