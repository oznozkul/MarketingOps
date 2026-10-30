namespace MarketingOps.Domain.Crawls;

/// <summary>Bir işaretleme sorununun sayfadaki sayısı ve ekibin öğeyi bulabileceği kısa örnekleri (en fazla beş).</summary>
public sealed record ElementIssue
{
    public const int MaxSamples = 5;

    public int Count { get; init; }
    public IReadOnlyList<string> Samples { get; init; } = [];

    public static ElementIssue None { get; } = new();
}
