using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

/// <summary>Açık ya da tekrar açılmış bir bulgunun skora giren hali. Sayfa sayısı 0 ise oran uygulanmaz.</summary>
public sealed record ScoreInput(Guid FindingId, string Title, Severity Severity, Guid? PageTypeId, int AffectedCount, int PageTypePageCount);
