using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class OpenGraphImageMetaRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MinWidth = new("minWidth", "En küçük görsel genişliği", "px", 1200m, 200m, 4000m);
    public static readonly RuleThreshold MinHeight = new("minHeight", "En küçük görsel yüksekliği", "px", 630m, 200m, 4000m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinWidth, MinHeight];
    public override string Code => "social.og.image_meta";
    public override string Name => "Paylaşım görselinin bilgisi eksik";
    public override string Description => "og:image varsa og:image:width, og:image:height ve og:image:alt etiketlerini ve bildirilen boyutun eşiğin üstünde olduğunu kontrol eder.";
    public override FindingCategory Category => FindingCategory.TechnicalSeo;
    public override Severity DefaultSeverity => Severity.Low;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "social:open-graph";
    public override string Impact => "Boyutu bildirilmeyen görsel ilk paylaşımda görünmeyebilir; küçük görsel büyük kart yerine küçük kartla gösterilir; açıklamasız görsel ekran okuyucuda boş kalır.";
    protected override string TitleTemplate => "{n} sayfada paylaşım görselinin bilgisi eksik";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Paylaşım görselinin boyutu ve açıklaması etiketlerle bildirilmeli; 1200×630 her platformda büyük kart verir.", "<meta property=\"og:image:width\" content=\"1200\" />\n<meta property=\"og:image:height\" content=\"630\" />\n<meta property=\"og:image:alt\" content=\"{Görselde ne olduğu}\" />");
    public override string TeamCriterion => "Paylaşım görselleri 1200×630 üretiliyor; boyut ve açıklama etiketleri şablonda.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsIndexable(page) || !PageChecks.HasMarkupFacts(page) || page.Data.OpenGraph is not { Image.Length: > 0 } og) yield break;
        var problems = new List<string>();
        if (!int.TryParse(og.ImageWidth, out var width) || !int.TryParse(og.ImageHeight, out var height)) problems.Add("boyut bildirilmemiş");
        else if (width < context.Threshold(MinWidth) || height < context.Threshold(MinHeight)) problems.Add($"{width}×{height} px");
        if (string.IsNullOrWhiteSpace(og.ImageAlt)) problems.Add("og:image:alt yok");
        if (problems.Count > 0) yield return Violation(page, string.Join(", ", problems), og.Image);
    }
}
