using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

/// <summary>
/// Açık ya da tekrar açılmış bir bulgunun skora giren hali. <see cref="PageCount"/> oranın paydasıdır: site skorunda sitenin,
/// sayfa tipi skorunda o tipin sayfa sayısı. 0 ise oran uygulanmaz, tam ağırlık düşer.
/// </summary>
public sealed record ScoreInput(Guid FindingId, string Title, Severity Severity, string RuleCode, Guid? PageTypeId, int AffectedCount, int PageCount);
