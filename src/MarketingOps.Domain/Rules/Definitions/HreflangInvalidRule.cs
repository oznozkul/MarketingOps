using System.Text.RegularExpressions;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Sayfadaki link[rel=alternate][hreflang] kümesini denetler: geçersiz dil/bölge kodu (ISO 639-1 + isteğe bağlı
/// ISO 3166-1 alpha-2 ya da x-default), aynı kodun farklı adres göstermesi ve kendine referans olmaması.
/// </summary>
public sealed partial class HreflangInvalidRule : RuleBase, IPageRule
{
    public override string Code => "seo.hreflang.invalid";
    public override string Name => "hreflang kümesi hatalı";
    public override string Description => "hreflang kodlarının geçerliliğini, aynı koda birden fazla adres verilmesini ve sayfanın kendine referans vermesini denetler.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:hreflang";
    public override string Impact => "Hatalı hreflang kümesi arama motorunun dil ve bölge sürümlerini eşlemesini bozar; sayfalar yanlış kitleye gösterilebilir.";
    protected override string TitleTemplate => "{n} sayfada hreflang kümesi hatalı";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her dil/bölge için tek adres verilmeli ve küme kendine referans içermeli.", "<link rel=\"alternate\" hreflang=\"tr\" href=\"https://alanadi.com/sayfa\" />\n<link rel=\"alternate\" hreflang=\"en\" href=\"https://alanadi.com/en/page\" />\n<link rel=\"alternate\" hreflang=\"x-default\" href=\"https://alanadi.com/sayfa\" />");
    public override string TeamCriterion => "hreflang kümesi geçerli kodlar taşıyor, her kod tek adresi gösteriyor ve kendine referans içeriyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasSignals(page)) yield break;
        var links = page.Data.Hreflangs;
        if (links.Count == 0) yield break;

        var issues = new List<string>();
        foreach (var link in links)
            if (!CodePattern().IsMatch(link.Code)) issues.Add($"Geçersiz kod: {link.Code}");
        foreach (var group in links.GroupBy(l => l.Code, StringComparer.OrdinalIgnoreCase))
            if (group.Select(l => Normalize(l.Url)).Distinct(StringComparer.Ordinal).Count() > 1)
                issues.Add($"Aynı kod farklı adres: {group.Key}");
        if (!links.Any(l => Normalize(l.Url) == page.NormalizedUrl))
            issues.Add("Kendine referans yok");

        if (issues.Count == 0) yield break;
        yield return Violation(page, issues[0], string.Join(" · ", issues.Distinct(StringComparer.Ordinal).Take(5)));
    }

    private static string? Normalize(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var uri) ? UrlKey.Normalize(uri) : null;

    [GeneratedRegex(@"^(x-default|[a-zA-Z]{2}(-[a-zA-Z]{2})?)$")]
    private static partial Regex CodePattern();
}
