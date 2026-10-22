using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class HtmlSizeRule : RuleBase, IPageRule
{
    public static readonly RuleThreshold MaxKb = new("maxKb", "En büyük HTML", "KB", 500m, 50m, 5000m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MaxKb];

    public override string Code => "perf.html.size";
    public override string Name => "HTML boyutu yüksek";
    public override string Description => "HTML yanıt boyutunun eşiği aşıp aşmadığını kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Page;
    public override string ReferenceKey => "perf-doc:p32";
    public override string Impact => "Büyük HTML'in indirilip ayrıştırılması uzun sürer; gömülü veri ve görseller ilk görüntüyü geciktirir.";
    protected override string TitleTemplate => "{n} sayfada HTML boyutu yüksek";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "HTML yanıtı küçük tutulmalı; büyük satır içi veri ve tekrar eden işaretlemeler azaltılmalı.", "1. Satır içi (inline) büyük script, stil ve SVG'leri harici dosyalara taşıyın.\n2. Sayfaya gömülü büyük JSON verisini azaltın ya da sonradan yükleyin.\n3. Listelerde gösterilen öğe sayısını ve tekrar eden işaretlemeyi azaltın.");
    public override string TeamCriterion => "Şablondaki büyük satır içi içerikler harici dosyalara taşındı.";

    public IEnumerable<RuleViolation> Evaluate(PageSnapshot page, RuleContext context)
    {
        if (!PageChecks.IsHtml(page) || page.HtmlSizeBytes is not { } bytes) yield break;
        var kb = bytes / 1024m;
        if (kb > context.Threshold(MaxKb)) yield return Violation(page, $"{kb.ToString("N0", Tr)} KB");
    }
}
