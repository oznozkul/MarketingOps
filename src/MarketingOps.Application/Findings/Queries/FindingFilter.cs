using MarketingOps.Domain.Findings;

namespace MarketingOps.Application.Findings.Queries;

/// <summary>
/// Bulgu listesinin filtreleri. Boş liste "filtre yok" demektir. <see cref="IncludeUnmatched"/> hiçbir sayfa tipine
/// uymayan sayfaların bulgularını sayfa tipi filtresine ekler.
/// </summary>
public sealed record FindingFilter(
    IReadOnlyList<Severity>? Severities = null,
    IReadOnlyList<FindingCategory>? Categories = null,
    IReadOnlyList<FindingStatus>? Statuses = null,
    IReadOnlyList<Guid>? PageTypeIds = null,
    bool IncludeUnmatched = false,
    IReadOnlyList<string>? RuleCodes = null,
    bool NewInLastCrawl = false,
    string? Search = null);
