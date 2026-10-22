using MarketingOps.Application.Findings.Queries;
using MarketingOps.Domain.Findings;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.ViewModels.Findings;

/// <summary>
/// Bulgu listesinin query string'i; paylaşılabilir. Durum verilmezse Açık + Tekrar açıldı varsayılır;
/// "status=any" tüm durumlar demektir (filtre formu bunu her zaman gönderir ki hiçbir kutu seçilmediğinde varsayılana dönülmesin).
/// </summary>
public sealed class FindingsQuery
{
    public const string AnyStatus = "any";
    public const string Unmatched = "unmatched";
    public static readonly IReadOnlyList<FindingStatus> DefaultStatuses = [FindingStatus.Open, FindingStatus.Reopened];

    [FromQuery(Name = "severity")] public List<Severity> Severities { get; set; } = [];
    [FromQuery(Name = "category")] public List<FindingCategory> Categories { get; set; } = [];
    [FromQuery(Name = "status")] public List<string> StatusValues { get; set; } = [];
    [FromQuery(Name = "pageType")] public List<string> PageTypes { get; set; } = [];
    [FromQuery(Name = "rule")] public List<string> Rules { get; set; } = [];
    [FromQuery(Name = "new")] public bool NewOnly { get; set; }
    [FromQuery(Name = "q")] public string? Q { get; set; }
    [FromQuery(Name = "sort")] public FindingSort Sort { get; set; }
    [FromQuery(Name = "desc")] public bool Desc { get; set; }
    [FromQuery(Name = "page")] public int Page { get; set; } = 1;
    [FromQuery(Name = "pageSize")] public int PageSize { get; set; } = 25;

    public IReadOnlyList<FindingStatus> Statuses => StatusValues.Count == 0
        ? DefaultStatuses
        : StatusValues.Select(v => Enum.TryParse<FindingStatus>(v, true, out var s) ? s : (FindingStatus?)null).OfType<FindingStatus>().Distinct().ToList();

    public IReadOnlyList<Guid> PageTypeIds => PageTypes.Select(v => Guid.TryParse(v, out var id) ? id : (Guid?)null).OfType<Guid>().ToList();
    public bool IncludeUnmatched => PageTypes.Contains(Unmatched, StringComparer.OrdinalIgnoreCase);
    public bool HasDefaultStatuses => Statuses.ToHashSet().SetEquals(DefaultStatuses);
    public string? Search => string.IsNullOrWhiteSpace(Q) ? null : Q.Trim();

    public bool IsDefault => Severities.Count == 0 && Categories.Count == 0 && PageTypes.Count == 0 && Rules.Count == 0
                             && !NewOnly && Search is null && HasDefaultStatuses;

    public FindingFilter ToFilter() => new(Severities, Categories, Statuses, PageTypeIds, IncludeUnmatched, Rules, NewOnly, Search);
}
