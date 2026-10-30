namespace MarketingOps.Domain.Crawls;

/// <summary>Başlık seviyeleri belge sırasıyla ve metni boş başlıklar.</summary>
public sealed record HeadingFacts
{
    public IReadOnlyList<int> Levels { get; init; } = [];
    public ElementIssue Empty { get; init; } = ElementIssue.None;

    public static HeadingFacts None { get; } = new();
}
