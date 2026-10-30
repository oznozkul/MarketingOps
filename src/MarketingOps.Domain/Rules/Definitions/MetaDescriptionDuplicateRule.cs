using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class MetaDescriptionDuplicateRule : RuleBase, ISiteRule
{
    public override string Code => "seo.meta_description.duplicate";
    public override string Name => "Meta description tekrarı";
    public override string Description => "Aynı meta description'ı paylaşan indekslenebilir sayfaları bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:snippets";
    public override string Impact => "Tekrar eden açıklamalar arama sonuçlarında sayfaları ayırt etmeyi zorlaştırır; arama motoru açıklamayı kendisi üretmeyi seçebilir.";
    protected override string TitleTemplate => "{n} sayfada meta description tekrarlanıyor";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Meta description her sayfaya özgü olmalı; aynı metin birçok sayfada tekrar etmemeli.", "{Sayfaya özgü bilgi} ile başlayan, {öne çıkan fayda} içeren kısa bir özet.\nŞablondaki sabit metni sayfaya özgü alanlarla (ebat, marka, model) zenginleştirin.");
    public override string TeamCriterion => "Meta description şablonu sayfaya özgü alan içerecek şekilde güncellendi.";

    public ISiteRuleRun Start(RuleContext context) => new DuplicateValueRun(Code, p => p.Data.MetaDescription);
}
