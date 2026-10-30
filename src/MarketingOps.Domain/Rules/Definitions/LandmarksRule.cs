using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class LandmarksRule : RuleBase, IPageRule
{
    public override string Code => "a11y.landmarks.missing";
    public override string Name => "Sayfa bölgeleri tanımsız";
    public override string Description => "Sayfada tam olarak bir <main> ve en az bir <header>, <nav> ya da <footer> bölgesi olup olmadığını kontrol eder (ARIA rolleri de sayılır).";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:landmarks";
    public override string Impact => "Bölgeleri tanımsız sayfada ekran okuyucu kullanıcısı menüyü geçip ana içeriğe atlayamaz; arama motoru da ana içeriği şablondan ayırmakta zorlanır.";
    protected override string TitleTemplate => "{n} sayfada sayfa bölgeleri tanımsız";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Ana şablon semantik bölgelerle kurulmalı; ana içerik tek bir <main> içinde olmalı.", "<header>…logo, arama…</header>\n<nav aria-label=\"Ana menü\">…</nav>\n<main>…sayfanın kendi içeriği…</main>\n<footer>…</footer>");
    public override string TeamCriterion => "Ana yerleşim şablonu header, nav, tek main ve footer kullanıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var landmarks = page.Data.Landmarks;
        var problems = new List<string>();
        if (landmarks.Main != 1) problems.Add(landmarks.Main == 0 ? "main yok" : $"{landmarks.Main} main");
        if (landmarks.Header + landmarks.Nav + landmarks.Footer == 0) problems.Add("header, nav ve footer yok");
        if (problems.Count > 0) yield return Violation(page, string.Join(", ", problems));
    }
}
