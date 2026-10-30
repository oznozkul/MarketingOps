using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ImageAltMissingRule : RuleBase, IPageRule
{
    public override string Code => "content.image.alt_missing";
    public override string Name => "Görselde alt etiketi eksik";
    public override string Description => "alt özniteliği hiç olmayan görselleri bulur. Boş alt (alt='') süs görseller için geçerli sayıldığından işaretlenmez.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:images";
    public override string Impact => "Alt metni görsel aramada görünürlüğü ve ekran okuyucu kullanıcılarının sayfayı anlamasını etkiler.";
    protected override string TitleTemplate => "{n} sayfada görsellerde alt etiketi eksik";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Görseller içeriği tanımlayan bir alt metin taşımalı.", "{Marka} {Model} {Ölçü} – {görselde ne olduğu}\nSüs amaçlı görsellerde alt=\"\" bırakın; ürün görsellerinde alt metni ürün alanlarından üretin.");
    public override string TeamCriterion => "Görsel şablonu alt metnini dolduracak şekilde güncellendi.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page)) yield break;
        var missing = page.Data.Images.Where(i => i.Alt is null && !string.IsNullOrWhiteSpace(i.Src)).ToList();
        if (missing.Count > 0) yield return Violation(page, $"{missing.Count} görsel", string.Join(", ", missing.Take(5).Select(i => i.Src)));
    }
}
