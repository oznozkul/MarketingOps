namespace MarketingOps.Application.PageTypes.Queries.GetPageTypeStats;

/// <summary>
/// <see cref="CrawlNumber"/> null ise henüz başarılı tarama yok ve sayılar bilinmiyor. Bir sayfa tipinde aktif kural sayısı:
/// sitede açık kurallar eksi o tipte yanlış alarmla susturulanlar (<see cref="ActiveRuleCount"/>).
/// </summary>
public sealed record PageTypeStatsResponse(
    int? CrawlNumber,
    Guid? CrawlId,
    IReadOnlyDictionary<Guid, int> Counts,
    int UnmatchedCount,
    int EnabledRuleCount,
    IReadOnlyDictionary<Guid, int> MutedRuleCounts)
{
    public int ActiveRuleCount(Guid pageTypeId) => EnabledRuleCount - MutedRuleCounts.GetValueOrDefault(pageTypeId);
}
