namespace MarketingOps.Domain.Crawls;

/// <summary>Sayfa bölgeleri (landmark) sayıları; birden fazla menü varsa etiketsiz olanlar.</summary>
public sealed record LandmarkFacts
{
    public int Main { get; init; }
    public int Header { get; init; }
    public int Nav { get; init; }
    public int Footer { get; init; }
    public ElementIssue UnlabeledNavs { get; init; } = ElementIssue.None;

    public static LandmarkFacts None { get; } = new();
}
