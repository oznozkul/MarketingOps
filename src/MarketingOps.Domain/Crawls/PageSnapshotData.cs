namespace MarketingOps.Domain.Crawls;

/// <summary>
/// HTML'den çıkarılan ve kuralların (Faz 3) ve performans kontrollerinin ihtiyaç duyduğu alanlar. jsonb olarak saklanır.
/// </summary>
public sealed record PageSnapshotData
{
    public string? Title { get; init; }
    public string? MetaDescription { get; init; }
    public string? Canonical { get; init; }
    public string? MetaRobots { get; init; }
    public IReadOnlyList<string> H1 { get; init; } = [];
    public IReadOnlyList<string> H2 { get; init; } = [];
    public string? Lang { get; init; }
    public string? Viewport { get; init; }
    public string? Charset { get; init; }
    public IReadOnlyList<string> SchemaTypes { get; init; } = [];
    public OpenGraphInfo? OpenGraph { get; init; }
    public IReadOnlyList<string> InternalLinks { get; init; } = [];
    public IReadOnlyList<string> ExternalLinks { get; init; } = [];
    public IReadOnlyList<ImageInfo> Images { get; init; } = [];
    public IReadOnlyList<ScriptInfo> Scripts { get; init; } = [];
    public IReadOnlyList<string> Stylesheets { get; init; } = [];
    public IReadOnlyList<PreloadInfo> Preloads { get; init; } = [];
    public IReadOnlyList<string> Preconnects { get; init; } = [];
    public int WordCount { get; init; }

    public static PageSnapshotData Empty { get; } = new();
}
