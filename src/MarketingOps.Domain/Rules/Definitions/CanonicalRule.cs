using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class CanonicalRule : RuleBase, IPageRule
{
    public override string Code => "seo.canonical.invalid";
    public override string Name => "Canonical eksik ya da başka domaine işaret ediyor";
    public override string Description => "Canonical bağlantısının varlığını ve sitenin kendi domaininde olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:canonical";
    public override string Impact => "Canonical olmayan sayfalarda parametreli kopyalar ayrı sayfa gibi indekslenebilir; başka domaine işaret eden canonical sayfayı dizinden düşürebilir.";
    protected override string TitleTemplate => "{n} sayfada canonical eksik ya da başka domaine işaret ediyor";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "İndekslenebilir her sayfa kendi kalıcı adresini gösteren bir canonical içermeli; canonical sitenin kendi domainine işaret etmeli.", "<link rel=\"canonical\" href=\"https://alanadi.com/sayfa-yolu\" />");
    public override string TeamCriterion => "Canonical etiketi şablona eklendi ve sitenin kendi domainine işaret ediyor.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page)) yield break;
        var canonical = page.Data.Canonical;
        if (canonical is null) yield return Violation(page, "Canonical yok");
        else if (Uri.TryCreate(canonical, UriKind.Absolute, out var url) && !context.IsInternal(url))
            yield return Violation(page, canonical, "Canonical başka bir domaine işaret ediyor.");
    }
}
