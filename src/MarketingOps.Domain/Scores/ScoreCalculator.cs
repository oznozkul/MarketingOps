using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

/// <summary>
/// Kategori skoru. 100'den başlar; her açık bulgu önem ağırlığı × etkilenen sayfa oranı kadar düşer.
/// Bulgu başına üst sınır ağırlığın kendisidir. Sonuç 0'ın altına inmez ve tam sayıya yuvarlanır.
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

    public static decimal Points(ScoreInput input)
    {
        var weight = Weight(input.Severity);
        if (input.PageTypeId is null || input.PageTypePageCount <= 0) return weight;
        var ratio = (decimal)input.AffectedCount / input.PageTypePageCount;
        return Math.Min(weight, weight * ratio);
    }

    public static int Score(IEnumerable<ScoreInput> findings)
    {
        var remaining = Start - findings.Sum(Points);
        if (remaining < 0) remaining = 0;
        return (int)Math.Round(remaining, MidpointRounding.AwayFromZero);
    }

    public static IReadOnlyList<ScoreDeduction> Breakdown(IEnumerable<ScoreInput> findings) =>
        findings
            .Select(f => new ScoreDeduction(f.FindingId, f.Title, f.Severity, f.PageTypeId, f.AffectedCount, f.PageTypePageCount, Points(f)))
            .OrderByDescending(d => d.Points)
            .ThenBy(d => d.Title)
            .ToList();
}
