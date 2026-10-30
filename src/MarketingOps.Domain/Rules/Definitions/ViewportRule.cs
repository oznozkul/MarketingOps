using System.Globalization;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ViewportRule : RuleBase, IPageRule
{
    private const decimal MinMaximumScale = 2m;

    public override string Code => "mobile.viewport.invalid";
    public override string Name => "Mobil viewport eksik ya da yakınlaştırmayı engelliyor";
    public override string Description => "meta viewport'un olup olmadığını, width=device-width içerdiğini ve yakınlaştırmayı (user-scalable=no ya da maximum-scale 2'den küçük) engellemediğini kontrol eder.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "mobile:viewport";
    public override string Impact => "Viewport'u olmayan sayfa telefonda masaüstü genişliğinde küçültülerek açılır; yakınlaştırmayı engellemek az gören kullanıcının metni büyütmesini imkânsız kılar.";
    protected override string TitleTemplate => "{n} sayfada mobil viewport hatalı";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Her sayfa yakınlaştırmayı engellemeyen standart viewport'u taşımalı.", "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
    public override string TeamCriterion => "Ana yerleşim şablonu standart viewport'u kullanıyor; user-scalable ve maximum-scale kaldırıldı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var viewport = page.Data.Viewport;
        if (string.IsNullOrWhiteSpace(viewport))
        {
            yield return Violation(page, "Viewport yok");
            yield break;
        }

        var values = viewport.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Split('=', 2))
            .Where(p => p.Length == 2)
            .GroupBy(p => p[0].Trim().ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Last()[1].Trim().ToLowerInvariant());
        var problems = new List<string>();
        if (values.GetValueOrDefault("width") != "device-width") problems.Add("width=device-width yok");
        if (values.GetValueOrDefault("user-scalable") is "no" or "0") problems.Add("yakınlaştırma kapalı");
        if (values.TryGetValue("maximum-scale", out var max) && decimal.TryParse(max, NumberStyles.Number, CultureInfo.InvariantCulture, out var scale) && scale < MinMaximumScale)
            problems.Add($"maximum-scale={max}");
        if (problems.Count > 0) yield return Violation(page, string.Join(", ", problems), viewport);
    }
}
