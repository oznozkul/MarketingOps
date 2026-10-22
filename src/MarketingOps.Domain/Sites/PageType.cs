using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Sites;

/// <summary>
/// URL deseni ve/veya schema tipiyle tanımlanan sayfa grubu. Site aggregate'inin child entity'sidir;
/// yalnızca <see cref="Site"/> üzerinden oluşturulur ve değiştirilir.
/// </summary>
public class PageType : Entity
{
    public const int NameMaxLength = 100;
    public const int MaxConditions = 20;

    private readonly List<MatchCondition> _conditions = [];

    public Guid SiteId { get; private set; }
    public string Name { get; private set; } = null!;
    public MatchMode Mode { get; private set; }
    public IReadOnlyList<MatchCondition> Conditions => _conditions;

    private PageType() { }

    internal static PageType Create(Guid siteId, string name, MatchMode mode, IEnumerable<MatchCondition> conditions)
    {
        var pageType = new PageType { SiteId = siteId };
        pageType.Apply(name, mode, conditions);
        return pageType;
    }

    internal void Update(string name, MatchMode mode, IEnumerable<MatchCondition> conditions) => Apply(name, mode, conditions);

    /// <summary>Crawler bu metotla sayfaya tip atar; ilk eşleşen tip kazanır.</summary>
    public bool Matches(Uri url, IReadOnlyCollection<string> schemaTypes) => Evaluate(Mode, _conditions, url, schemaTypes);

    /// <summary>Kaydedilmemiş koşulların önizlemesi de aynı eşleşme mantığını kullansın diye statik.</summary>
    public static bool Evaluate(MatchMode mode, IReadOnlyCollection<MatchCondition> conditions, Uri url, IReadOnlyCollection<string> schemaTypes) =>
        conditions.Count > 0 && (mode == MatchMode.All
            ? conditions.All(c => c.IsMatch(url, schemaTypes))
            : conditions.Any(c => c.IsMatch(url, schemaTypes)));

    private void Apply(string name, MatchMode mode, IEnumerable<MatchCondition> conditions)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Sayfa tipi adı boş olamaz.");

        // Kopyalanır: aynı value object örneği iki sayfa tipine verilirse EF owned type olarak yalnızca birine bağlanır.
        var list = conditions.Distinct().Select(c => c with { }).ToList();
        if (list.Count == 0)
            throw new DomainException("Sayfa tipinin en az bir eşleşme koşulu olmalı.");
        if (list.Count > MaxConditions)
            throw new DomainException($"Bir sayfa tipinde en fazla {MaxConditions} koşul olabilir.");

        Name = name.Trim();
        Mode = mode;
        _conditions.Clear();
        _conditions.AddRange(list);
    }
}
