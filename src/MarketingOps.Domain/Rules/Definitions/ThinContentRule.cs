using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ThinContentRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinWords = new("minWords", "En az kelime", "kelime", 250m, 50m, 2000m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinWords];

    public override string Code => "content.thin";
    public override string Name => "Zayıf içerik";
    public override string Description => "Ana içerikteki kelime sayısının eşiğin altında olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:helpful-content";
    public override string Impact => "Açıklayıcı metni olmayan sayfalar benzer sayfalardan ayrışamaz ve arama motoru için düşük değerli görünebilir.";
    protected override string TitleTemplate => "{n} sayfada içerik zayıf";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Sayfa, ziyaretçinin sorusunu yanıtlayan özgün bir içerik taşımalı.", "{Sayfa konusu} için: ne olduğu, kimin için uygun olduğu, nasıl seçileceği ve sık sorulan sorular.\nŞablon metni yerine sayfa tipine özgü açıklama blokları ekleyin.");
    public override string TeamCriterion => "Sayfa tipine özgün açıklama içeriği eklendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (PageChecks.IsIndexable(page) && page.Data.WordCount < context.Threshold(MinWords))
            yield return Violation(page, $"{page.Data.WordCount.ToString("N0", Tr)} kelime");
    }
}
