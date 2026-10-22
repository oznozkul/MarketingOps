using MarketingOps.Domain.Crawls;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Tekrar eden metin kuralları (title, meta description) için ortak birikim: değer → sayfalar.</summary>
public sealed class DuplicateValueRun(string ruleCode, Func<PageSnapshot, string?> select) : ISiteRuleRun
{
    private readonly Dictionary<string, List<(string Url, Guid? PageTypeId, string Value)>> _byValue = new(StringComparer.OrdinalIgnoreCase);

    public void Observe(PageSnapshot page)
    {
        if (!PageChecks.IsIndexable(page) || select(page) is not { } value || string.IsNullOrWhiteSpace(value)) return;
        var key = value.Trim();
        if (!_byValue.TryGetValue(key, out var pages)) _byValue[key] = pages = [];
        pages.Add((page.Url, page.PageTypeId, key));
    }

    public IEnumerable<RuleViolation> Complete() =>
        _byValue.Values
            .Where(pages => pages.Count > 1)
            .SelectMany(pages => pages.Select(p => new RuleViolation(ruleCode, p.PageTypeId, p.Url, p.Value, $"{pages.Count} sayfada aynı değer")));
}
