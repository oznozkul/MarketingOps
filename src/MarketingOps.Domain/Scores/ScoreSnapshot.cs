using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Scores;

/// <summary>Bir değerlendirmenin kategori skoru. Aynı günün sonraki değerlendirmesi satırı günceller.</summary>
public class ScoreSnapshot : Entity
{
    public Guid SiteId { get; private set; }
    public ScoreCategory Category { get; private set; }
    public int Score { get; private set; }
    public DateOnly Date { get; private set; }
    public DateTimeOffset TakenAt { get; private set; }

    private ScoreSnapshot() { }

    public static ScoreSnapshot Create(Guid siteId, ScoreCategory category, int score, DateOnly date, DateTimeOffset takenAt)
    {
        var row = new ScoreSnapshot { SiteId = siteId, Category = category, Date = date };
        row.Replace(score, takenAt);
        return row;
    }

    public void Replace(int score, DateTimeOffset takenAt)
    {
        if (score is < 0 or > ScoreCalculator.Start) throw new DomainException("Skor 0 ile 100 arasında olmalı.");
        Score = score;
        TakenAt = takenAt;
    }
}
