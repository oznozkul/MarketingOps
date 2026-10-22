namespace MarketingOps.Web.ViewModels.Design;

/// <summary>/_design vitrini. Veriler örnektir; gerçek ekranlar Application sorgularını kullanır.</summary>
public sealed record DesignViewModel(
    IReadOnlyList<ScoreCardViewModel> Scores,
    IReadOnlyList<DesignFindingRow> Rows,
    int Page,
    int PageSize,
    int Total,
    string Sort,
    bool Descending,
    IReadOnlyList<DesignOrderDay> Orders,
    IReadOnlyList<DesignTrendPoint> Trend,
    IReadOnlyList<DesignTimelineEvent> Events);
