using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Sites;

public class Site : Entity
{
    public const int NameMaxLength = 200;
    public const int DomainMaxLength = 253;
    public const int MaxPageTypes = 100;
    public const int TaskKeyPrefixMaxLength = 10;

    private readonly List<PageType> _pageTypes = [];

    public Guid OrganizationId { get; private set; }
    public string Name { get; private set; } = null!;
    public string Domain { get; private set; } = null!;
    public bool IsOwnershipVerified { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public CrawlSettings CrawlSettings { get; private set; } = CrawlSettings.Default;
    public ExpectedTrackingIds ExpectedTrackingIds { get; private set; } = ExpectedTrackingIds.None;

    /// <summary>Görev anahtarlarının öneki ("LB" → "LB-142").</summary>
    public string TaskKeyPrefix { get; private set; } = null!;
    public int LastTaskNumber { get; private set; }
    public IReadOnlyList<PageType> PageTypes => _pageTypes;

    private Site() { }

    public static Site Create(Guid organizationId, string name, string domain, DateTimeOffset now) => new()
    {
        OrganizationId = organizationId,
        Name = RequireName(name),
        Domain = NormalizeDomain(domain),
        TaskKeyPrefix = DerivePrefix(name),
        CreatedAt = now
    };

    /// <summary>
    /// "https://www.Ornek.com/" → "ornek.com". Aynı sitenin farklı yazımları tek kayda düşsün diye
    /// karşılaştırma ve benzersizlik kontrolü normalize edilmiş host üzerinden yapılır.
    /// </summary>
    public static string NormalizeDomain(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new DomainException("Domain boş olamaz.");

        var value = domain.Trim().ToLowerInvariant();
        var schemeEnd = value.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd >= 0) value = value[(schemeEnd + 3)..];

        var pathStart = value.IndexOfAny(['/', '?', '#']);
        if (pathStart >= 0) value = value[..pathStart];
        if (value.StartsWith("www.", StringComparison.Ordinal)) value = value[4..];

        if (value.Length is 0 or > DomainMaxLength || Uri.CheckHostName(value) != UriHostNameType.Dns || !value.Contains('.'))
            throw new DomainException($"Geçersiz domain: {domain.Trim()}");

        return value;
    }

    public void Rename(string name) => Name = RequireName(name);

    public void UpdateCrawlSettings(CrawlSettings settings) => CrawlSettings = settings;

    public void UpdateExpectedTrackingIds(ExpectedTrackingIds ids) => ExpectedTrackingIds = ids;

    public void MarkOwnershipVerified() => IsOwnershipVerified = true;

    public void UpdateTaskKeyPrefix(string prefix)
    {
        var value = prefix?.Trim().ToUpperInvariant() ?? "";
        if (value.Length is 0 or > TaskKeyPrefixMaxLength || !value.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9') || !char.IsLetter(value[0]))
            throw new DomainException($"Görev öneki harfle başlamalı ve yalnızca A–Z ve rakam içermeli (en fazla {TaskKeyPrefixMaxLength} karakter).");
        TaskKeyPrefix = value;
    }

    /// <summary>Site içinde artan görev numarası ve anahtarı. Önek değişse de numara sırası korunur.</summary>
    public (int Number, string Key) AllocateTaskKey()
    {
        // Görev özelliğinden önce oluşturulmuş sitelerde önek boştur; ilk görevde addan türetilir.
        if (string.IsNullOrEmpty(TaskKeyPrefix)) TaskKeyPrefix = DerivePrefix(Name);
        LastTaskNumber++;
        return (LastTaskNumber, $"{TaskKeyPrefix}-{LastTaskNumber}");
    }

    /// <summary>"Lastik Borsası" → "LB", "Tyreland" → "TYR". Türkçe harfler ASCII karşılığına çevrilir.</summary>
    public static string DerivePrefix(string name)
    {
        static char Ascii(char c) => char.ToUpperInvariant(c) switch { 'Ç' => 'C', 'Ğ' => 'G', 'İ' or 'I' => 'I', 'Ö' => 'O', 'Ş' => 'S', 'Ü' => 'U', var u => u };
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => new string(w.Select(Ascii).Where(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9').ToArray()))
            .Where(w => w.Length > 0 && char.IsLetter(w[0]))
            .ToList();
        var prefix = words.Count switch
        {
            0 => "GRV",
            1 => words[0][..Math.Min(3, words[0].Length)],
            _ => string.Concat(words.Take(4).Select(w => w[0]))
        };
        return prefix;
    }

    public PageType AddPageType(string name, MatchMode mode, IEnumerable<MatchCondition> conditions)
    {
        if (_pageTypes.Count >= MaxPageTypes)
            throw new DomainException($"Bir sitede en fazla {MaxPageTypes} sayfa tipi olabilir.");
        EnsureUniqueName(name, exceptId: null);

        var pageType = PageType.Create(Id, name, mode, conditions);
        _pageTypes.Add(pageType);
        return pageType;
    }

    public void UpdatePageType(Guid pageTypeId, string name, MatchMode mode, IEnumerable<MatchCondition> conditions)
    {
        var pageType = FindPageType(pageTypeId);
        EnsureUniqueName(name, exceptId: pageTypeId);
        pageType.Update(name, mode, conditions);
    }

    public void RemovePageType(Guid pageTypeId) => _pageTypes.Remove(FindPageType(pageTypeId));

    /// <summary>URL'e uyan sayfa tipleri. Birden fazla tip uyuyorsa crawler ilkini seçer; ekran bunu çakışma olarak gösterir.</summary>
    public IReadOnlyList<PageType> FindMatchingPageTypes(Uri url, IReadOnlyCollection<string> schemaTypes, Guid? exceptPageTypeId = null) =>
        _pageTypes.Where(p => p.Id != exceptPageTypeId && p.Matches(url, schemaTypes)).ToList();

    private PageType FindPageType(Guid pageTypeId) =>
        _pageTypes.FirstOrDefault(p => p.Id == pageTypeId)
        ?? throw new DomainException("Sayfa tipi bu sitede bulunamadı.");

    private void EnsureUniqueName(string name, Guid? exceptId)
    {
        var trimmed = name?.Trim() ?? "";
        if (_pageTypes.Any(p => p.Id != exceptId && string.Equals(p.Name, trimmed, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Bu sitede \"{trimmed}\" adında bir sayfa tipi zaten var.");
    }

    private static string RequireName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Site adı boş olamaz.");
        return name.Trim();
    }
}
