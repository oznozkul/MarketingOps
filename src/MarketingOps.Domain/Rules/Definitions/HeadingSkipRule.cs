using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class HeadingSkipRule : RuleBase, IPageRule
{
    public override string Code => "a11y.heading.skip";
    public override string Name => "Başlık seviyesi atlanıyor ya da boş başlık";
    public override string Description => "Başlıkların sırayla ilerleyip ilerlemediğini (H2'den sonra H4 gelmemeli) ve metni boş başlık olup olmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Accessibility;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "a11y:headings";
    public override string Impact => "Ekran okuyucu kullanıcıları sayfada başlıklarla gezinir; atlanan seviye ve boş başlık sayfanın yapısını bozar, arama motoru da konunun hiyerarşisini yanlış okur.";
    protected override string TitleTemplate => "{n} sayfada başlık yapısı bozuk";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Başlıklar görünüşe göre değil, içerik hiyerarşisine göre seçilmeli.", "1. Sayfada tek H1, bölümler H2, alt bölümler H3 olacak şekilde sırayı düzeltin.\n2. Başlığı yalnızca büyük yazı için kullanan yerlerde başlık etiketi yerine CSS sınıfı kullanın.\n3. Metni olmayan başlık etiketlerini kaldırın.");
    public override string TeamCriterion => "Şablonlarda başlık seviyeleri sırayla ilerliyor; boş başlık yok.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !PageChecks.HasMarkupFacts(page)) yield break;
        var headings = page.Data.Headings;
        var skips = Skips(headings.Levels).ToList();
        var problems = new List<string>();
        if (skips.Count > 0) problems.Add(string.Join(", ", skips.Distinct().Select(s => $"H{s.From} → H{s.To}")));
        if (headings.Empty.Count > 0) problems.Add($"{headings.Empty.Count} boş başlık");
        if (problems.Count > 0)
            yield return Violation(page, string.Join(" · ", problems), "Sıra: " + string.Join(" ", headings.Levels.Take(20).Select(l => $"H{l}")));
    }

    private static IEnumerable<(int From, int To)> Skips(IReadOnlyList<int> levels)
    {
        for (var i = 1; i < levels.Count; i++)
            if (levels[i] > levels[i - 1] + 1) yield return (levels[i - 1], levels[i]);
    }
}
