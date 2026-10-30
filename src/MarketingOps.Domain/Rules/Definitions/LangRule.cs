using System.Text.RegularExpressions;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed partial class LangRule : RuleBase, IPageRule
{
    public override string Code => "a11y.lang.missing";
    public override string Name => "Sayfa dili tanımsız";
    public override string Description => "<html lang> özniteliğinin olup olmadığını ve geçerli bir dil kodu (tr, tr-TR, en …) olduğunu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:lang";
    public override string Impact => "Dili tanımsız sayfayı ekran okuyucu yanlış dilin telaffuzuyla okur; tarayıcı çeviri önerisi ve arama motorunun dil tespiti de bundan etkilenir.";
    protected override string TitleTemplate => "{n} sayfada sayfa dili tanımsız";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Kök öğe sayfanın dilini taşımalı.", "<html lang=\"tr\">");
    public override string TeamCriterion => "Ana yerleşim şablonu <html lang> değerini sayfanın diline göre yazıyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var lang = page.Data.Lang?.Trim();
        if (string.IsNullOrEmpty(lang)) yield return Violation(page, "lang yok");
        else if (!LanguageTag().IsMatch(lang)) yield return Violation(page, $"Geçersiz: \"{lang}\"");
    }

    [GeneratedRegex(@"^[a-zA-Z]{2,3}(-[a-zA-Z0-9]{2,8})*$")]
    private static partial Regex LanguageTag();
}
