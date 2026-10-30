using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class GenericLinkTextRule : RuleBase, IPageRule
{
    private static readonly HashSet<string> English =
        ["click here", "here", "read more", "more", "link", "this", "learn more", "details", "click", "go", "page"];

    private static readonly HashSet<string> Turkish =
        ["tıkla", "tıklayın", "buraya tıklayın", "buraya tıkla", "devamı", "devamını oku", "daha fazla", "daha fazlası", "detay", "detaylar",
         "incele", "link", "burada", "burası", "git", "tümü", "hepsi"];

    public override string Code => "seo.link.generic_text";
    public override string Name => "Jenerik link metni";
    public override string Description => "\"Buraya tıklayın\", \"devamı\", \"read more\" gibi gittiği yeri anlatmayan link metinlerini bulur. Türkçe sayfada iki dilin listesi de kullanılır.";
    public override FindingCategory Category => FindingCategory.Content;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "google:links";
    public override string Impact => "Link metni, hedef sayfanın konusunu arama motoruna ve ekran okuyucu kullanıcılarına anlatır; jenerik metin bu bilgiyi boşa harcar.";
    protected override string TitleTemplate => "{n} sayfada jenerik link metni var";
    public override Team Team => Team.Content;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Text, "Link metni gittiği sayfayı tek başına anlatmalı.", "\"Devamını oku\" yerine \"{Yazı başlığı} yazısının devamı\"\n\"İncele\" yerine \"{Ürün adı} ürününü incele\"");
    public override string TeamCriterion => "Şablonlardaki jenerik link metinleri hedefi anlatan metinlerle değişti.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var turkish = page.Data.Lang?.StartsWith("tr", StringComparison.OrdinalIgnoreCase) == true;
        var generic = page.Data.Links.ShortTexts.Where(t => English.Contains(t.Text) || (turkish && Turkish.Contains(t.Text))).ToList();
        if (generic.Count == 0) yield break;
        yield return Violation(page, $"{generic.Sum(t => t.Count).ToString("N0", Tr)} link", string.Join(" · ", generic.Select(t => $"\"{t.Text}\" ×{t.Count}")));
    }
}
