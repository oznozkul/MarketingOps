namespace MarketingOps.Domain.Crawls;

/// <summary>Veri tabloları (role=presentation/none hariç) ve başlık hücresi olmayanlar.</summary>
public sealed record TableFacts
{
    public int Tables { get; init; }
    public ElementIssue WithoutHeader { get; init; } = ElementIssue.None;

    public static TableFacts None { get; } = new();
}
