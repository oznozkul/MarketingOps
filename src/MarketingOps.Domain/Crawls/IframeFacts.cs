namespace MarketingOps.Domain.Crawls;

public sealed record IframeFacts
{
    public int Iframes { get; init; }
    public ElementIssue WithoutTitle { get; init; } = ElementIssue.None;
    public ElementIssue NotLazy { get; init; } = ElementIssue.None;

    public static IframeFacts None { get; } = new();
}
