namespace MarketingOps.Domain.Crawls;

/// <summary>Sayfadaki yapılandırılmış veri: JSON-LD blokları, düğümleri ve microdata tipleri.</summary>
public sealed record StructuredDataInfo
{
    public int JsonLdBlocks { get; init; }
    public ElementIssue Invalid { get; init; } = ElementIssue.None;
    public IReadOnlyList<SchemaNode> Nodes { get; init; } = [];
    public IReadOnlyList<string> MicrodataTypes { get; init; } = [];

    /// <summary>Geçerli en az bir JSON-LD düğümü ya da microdata var. Metot: jsonb'ye yazılmasın.</summary>
    public bool IsPresent() => Nodes.Count > 0 || MicrodataTypes.Count > 0;

    public static StructuredDataInfo Empty { get; } = new();
}
