using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class TitleDuplicateRule : RuleBase, ISiteRule
{
    public override string Code => "seo.title.duplicate";
    public override string Name => "Title tekrarı";
    public override string Description => "Aynı title'ı paylaşan indekslenebilir sayfaları bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "google:title-links";
    public override string Impact => "Aynı title'ı taşıyan sayfalar arama sonuçlarında birbirinden ayırt edilemez ve aynı sorgu için birbiriyle yarışır.";
    protected override string TitleTemplate => "{n} sayfada title tekrarlanıyor";
    public override Team Team => Team.Seo;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Title, sayfayı diğerlerinden ayıran bilgiyi (ölçü, marka, model, kategori) içermeli.", "{Ayırt edici bilgi} {Sayfa türü} | {Marka}\nŞablona sayfaya özgü alanı (ör. ebat ya da model adı) ekleyin; sabit metinle biten title'ları kaldırın.");
    public override string TeamCriterion => "Title şablonu sayfaya özgü bir alan içerecek şekilde güncellendi.";

    public ISiteRuleRun Start(RuleContext context) => new DuplicateValueRun(Code, p => p.Data.Title);
}
