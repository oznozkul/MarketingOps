namespace MarketingOps.Domain.Crawls;

/// <summary>
/// JSON-LD'deki bir düğüm. <see cref="Properties"/>: değeri dolu olan özellik yolları, iç içe nesneler noktayla
/// (<c>offers.price</c>); dizilerde öğelerin birleşimi. <see cref="TopLevel"/>: kökte ya da @graph'ta duran düğüm.
/// </summary>
public sealed record SchemaNode
{
    public string Type { get; init; } = "";
    public bool TopLevel { get; init; }
    public IReadOnlyList<string> Properties { get; init; } = [];
}
