using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class PreconnectRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MaxPreconnects = new("maxPreconnects", "En fazla preconnect", "adet", 4m, 1m, 20m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MaxPreconnects];

    public override string Code => "perf.preconnect";
    public override string Name => "Preconnect eksik ya da fazla";
    public override string Description => "<head>'deki kritik dış kaynaklar için preconnect olup olmadığını ve preconnect sayısının fazla olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p24";
    public override string Impact => "Eksik preconnect kritik dosyaların bağlantı kurulumunu geciktirir; fazla preconnect bağlantıları boşa açar.";
    protected override string TitleTemplate => "{n} sayfada preconnect eksik ya da fazla";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Kritik üçüncü taraf kaynaklara erken bağlantı kurulmalı; gereksiz preconnect'ler kaldırılmalı.", "<link rel=\"preconnect\" href=\"https://cdn.ornek.com\" crossorigin />");
    public override string TeamCriterion => "Preconnect listesi yalnızca kritik kaynakları içerecek şekilde düzenlendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var preconnected = page.Data.Preconnects.Select(Origin).OfType<string>().ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (preconnected.Count > context.Threshold(MaxPreconnects))
        {
            yield return Violation(page, $"{preconnected.Count} preconnect", "Fazla preconnect: " + string.Join(", ", preconnected));
            yield break;
        }
        var missing = page.Data.Stylesheets
            .Concat(page.Data.Scripts.Where(s => s.InHead).Select(s => s.Src).OfType<string>())
            .Where(u => Uri.TryCreate(u, UriKind.Absolute, out var url) && !context.IsInternal(url))
            .Select(Origin).OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(o => !preconnected.Contains(o))
            .ToList();
        if (missing.Count > 0) yield return Violation(page, $"{missing.Count} kaynak", "Preconnect eksik: " + string.Join(", ", missing));
    }

    private static string? Origin(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out var u) ? $"{u.Scheme}://{u.Host}" : null;
}
