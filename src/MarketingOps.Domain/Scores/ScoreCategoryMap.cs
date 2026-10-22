using MarketingOps.Domain.Findings;

namespace MarketingOps.Domain.Scores;

public static class ScoreCategoryMap
{
    public static bool Includes(ScoreCategory score, FindingCategory finding) => score switch
    {
        ScoreCategory.Tracking => finding == FindingCategory.Tracking,
        ScoreCategory.Seo => finding is FindingCategory.TechnicalSeo or FindingCategory.Content,
        _ => finding == FindingCategory.Performance
    };
}
