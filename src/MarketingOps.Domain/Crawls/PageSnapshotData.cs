namespace MarketingOps.Domain.Crawls;

/// <summary>
/// HTML'den çıkarılan ve kuralların (Faz 3) ve performans kontrollerinin ihtiyaç duyduğu alanlar. jsonb olarak saklanır.
/// </summary>
public sealed record PageSnapshotData
{
    /// <summary>
    /// Ayrıştırıcının sürümü. 2: Faz 8'in işaretleme alanları (<see cref="StructuredData"/> ve sonrası) dolu.
    /// Eski kayıtlarda 0'dır; bu alanlara bakan kurallar eski kayıtları değerlendirmez.
    /// </summary>
    public int Version { get; init; }

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
    public StructuredDataInfo StructuredData { get; init; } = StructuredDataInfo.Empty;
    public LinkFacts Links { get; init; } = LinkFacts.Empty;
    public HeadingFacts Headings { get; init; } = HeadingFacts.None;
    public FormFacts Forms { get; init; } = FormFacts.None;
    public TableFacts Tables { get; init; } = TableFacts.None;
    public IframeFacts Iframes { get; init; } = IframeFacts.None;
    public LandmarkFacts Landmarks { get; init; } = LandmarkFacts.None;

    public const int MarkupVersion = 2;

    public static PageSnapshotData Empty { get; } = new();
}
