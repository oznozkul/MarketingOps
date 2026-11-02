using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// HTTPS sayfada http:// ile yüklenen görsel, script ve stylesheet. Göreli ve protokolsüz ("//cdn...") adresler sayfanın
/// şemasını alır, sayılmaz. Görsel adresi ayrıştırıcıda ham saklandığı için önek kontrolü yapılır.
/// </summary>
public sealed class MixedContentRule : RuleBase, IPageRule
{
    private const int MaxSamples = 5;

    public override string Code => "sec.mixed_content";
    public override string Name => "Karışık içerik";
    public override string Description => "HTTPS sayfada http:// üzerinden yüklenen görsel, script ve stylesheet'leri bulur.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "security:links";
    public override string Impact => "Tarayıcılar HTTPS sayfadaki http script ve stylesheet'leri engeller; sayfa bozuk görünebilir ya da çalışmayabilir. Görseller için adres çubuğunda güvenli değil uyarısı çıkar.";
    protected override string TitleTemplate => "{n} sayfada karışık içerik var";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Kaynakları https:// ile yükleyin.",
        "1. Ayrıntıdaki http:// adreslerin https karşılığını açın; çalışıyorsa şablonda ve içerikte https'e çevirin.\n2. https sunmayan kaynağı kendi alan adınıza taşıyın ya da kaldırın.\n3. İçerik yönetim sistemindeki eski içerikte http:// geçen görsel adreslerini toplu güncelleyin.");
    public override string TeamCriterion => "HTTPS sayfalarda http:// ile yüklenen kaynak kalmadı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || !page.Url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) yield break;

        var insecure = page.Data.Images.Select(i => i.Src)
            .Concat(page.Data.Scripts.Select(s => s.Src).OfType<string>())
            .Concat(page.Data.Stylesheets)
            .Where(src => src.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (insecure.Count > 0)
            yield return Violation(page, $"{insecure.Count.ToString("N0", Tr)} kaynak", string.Join(" · ", insecure.Take(MaxSamples)));
    }
}
