namespace MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;

/// <summary>
/// <see cref="CrawlNumber"/> null ise değerlendirilmiş tarama yok. <see cref="AffectedFindings"/>: sayfa sayısı değişecek bulgu
/// (sayfa tipi grubu) sayısı; açılacak ve kapanacak bulgular dahil.
/// </summary>
public sealed record ThresholdImpactResponse(
    int? CrawlNumber,
    int CurrentPages,
    int NewPages,
    int CurrentFindings,
    int NewFindings,
    int AffectedFindings);
