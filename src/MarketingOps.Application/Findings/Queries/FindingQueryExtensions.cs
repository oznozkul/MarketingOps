using System.Linq.Expressions;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Findings.Queries;

/// <summary>Liste, özet ve dışa aktarma sorgularının paylaştığı filtre ve sıralama.</summary>
public static class FindingQueryExtensions
{
    /// <summary>"Son taramada yeni" için kural değerlendirmesi yapılmış en son tarama.</summary>
    public static Task<Guid?> LatestEvaluatedCrawlIdAsync(this IAppDbContext db, Guid siteId, CancellationToken ct) =>
        db.Crawls.AsNoTracking()
            .Where(c => c.SiteId == siteId && c.EvaluatedAt != null)
            .OrderByDescending(c => c.Number)
            .Select(c => (Guid?)c.Id)
            .FirstOrDefaultAsync(ct);

    public static IQueryable<Finding> Filter(this IQueryable<Finding> findings, FindingFilter filter, Guid? latestCrawlId)
    {
        if (filter.Severities is { Count: > 0 } severities) findings = findings.Where(f => severities.Contains(f.Severity));
        if (filter.Categories is { Count: > 0 } categories) findings = findings.Where(f => categories.Contains(f.Category));
        if (filter.Statuses is { Count: > 0 } statuses) findings = findings.Where(f => statuses.Contains(f.Status));
        if (filter.RuleCodes is { Count: > 0 } codes) findings = findings.Where(f => codes.Contains(f.RuleCode));

        var pageTypeIds = filter.PageTypeIds ?? [];
        if (pageTypeIds.Count > 0 || filter.IncludeUnmatched)
            findings = findings.Where(f => (f.PageTypeId != null && pageTypeIds.Contains(f.PageTypeId.Value)) || (filter.IncludeUnmatched && f.PageTypeId == null));

        if (filter.NewInLastCrawl)
            findings = latestCrawlId is { } crawlId
                ? findings.Where(f => f.LastOpenedCrawlId == crawlId && (f.Status == FindingStatus.Open || f.Status == FindingStatus.Reopened))
                : findings.Where(_ => false);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLowerInvariant();
            findings = findings.Where(f => f.Title.ToLower().Contains(term) || f.RuleCode.ToLower().Contains(term));
        }
        return findings;
    }

    public static IQueryable<Finding> Sort(this IQueryable<Finding> findings, FindingSort sort, bool descending) => sort switch
    {
        FindingSort.Title => Order(findings, f => f.Title, descending),
        FindingSort.Category => Order(findings, f => f.Category, descending),
        FindingSort.AffectedCount => Order(findings, f => f.AffectedCount, !descending),
        FindingSort.FirstSeen => Order(findings, f => f.FirstSeenAt, descending),
        FindingSort.LastSeen => Order(findings, f => f.LastSeenAt, descending),
        FindingSort.Status => Order(findings, StatusRank, descending),
        _ => Order(findings, SeverityRank, descending).ThenByDescending(f => f.AffectedCount)
    };

    // Önem ve durum metin olarak saklanır; anlamlı sıralama için sıra numarasına çevrilir.
    private static readonly Expression<Func<Finding, int>> SeverityRank = f =>
        f.Severity == Severity.Critical ? 0 : f.Severity == Severity.High ? 1 : f.Severity == Severity.Medium ? 2 : 3;

    private static readonly Expression<Func<Finding, int>> StatusRank = f =>
        f.Status == FindingStatus.Reopened ? 0 : f.Status == FindingStatus.Open ? 1 : f.Status == FindingStatus.Snoozed ? 2
        : f.Status == FindingStatus.FalsePositive ? 3 : 4;

    private static IOrderedQueryable<Finding> Order<TKey>(IQueryable<Finding> query, Expression<Func<Finding, TKey>> key, bool descending) =>
        descending ? query.OrderByDescending(key) : query.OrderBy(key);
}
