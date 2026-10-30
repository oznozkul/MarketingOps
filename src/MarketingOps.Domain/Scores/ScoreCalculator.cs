using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

/// <summary>
/// Kategori skoru. 100'den başlar; her açık bulgu önem ağırlığı × etkilenen sayfa oranı kadar düşer. Oranın paydası
/// skora göre değişir: site skorunda sitenin, sayfa tipi skorunda o tipin sayfa sayısı.
/// Bir kuralın düşürebileceği puan, bütün sayfa tiplerindeki bulguları birlikte, en yüksek önem ağırlığını aşmaz;
/// şablondan gelen tek bir sorun yedi sayfa tipinde yedi kez cezalandırılmaz. Aşan kuralda bulguların payı orantılı küçülür.
/// Sayfaya dayanmayan bulgu (sayfa sayısı 0: purchase farkı, örnek sayfa ölçümleri) tam ağırlık düşürür.
/// Sonuç 0'ın altına inmez ve tam sayıya yuvarlanır.
/// </summary>
public static class ScoreCalculator
{
    public const int Start = 100;

    public static int Weight(Severity severity) => severity switch
    {
        Severity.Critical => 15,
        Severity.High => 8,
        Severity.Medium => 3,
        _ => 1
    };

    /// <summary>Kural sınırı uygulanmadan bulgunun payı. Sayfa sayısı yoksa (sayfaya dayanmayan bulgu) tam ağırlık.</summary>
    public static decimal Points(ScoreInput input)
    {
        var weight = Weight(input.Severity);
        if (input.PageCount <= 0) return weight;
        return Math.Min(weight, weight * (decimal)input.AffectedCount / input.PageCount);
    }

    public static int Score(IEnumerable<ScoreInput> findings)
    {
        var remaining = Start - Breakdown(findings).Sum(d => d.Points);
        if (remaining < 0) remaining = 0;
        return (int)Math.Round(remaining, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<ScoreDeduction> Breakdown(IEnumerable<ScoreInput> findings) =>
        findings
            .GroupBy(f => f.RuleCode)
            .SelectMany(CappedByRule)
            .OrderByDescending(d => d.Points)
            .ThenBy(d => d.Title)
            .ToList();

    private static IEnumerable<ScoreDeduction> CappedByRule(IGrouping<string, ScoreInput> rule)
    {
        var raw = rule.Select(f => (Input: f, Points: Points(f))).ToList();
        var cap = rule.Max(f => Weight(f.Severity));
        var total = raw.Sum(r => r.Points);
        var scale = total > cap ? cap / total : 1m;
        return raw.Select(r => new ScoreDeduction(r.Input.FindingId, r.Input.Title, r.Input.Severity, r.Input.PageTypeId,
            r.Input.AffectedCount, r.Input.PageCount, r.Points * scale));
    }
}
