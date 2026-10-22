using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class LcpRule : WebVitalRule
{
    public static readonly RuleThreshold Max = new("maxMs", "En yüksek LCP", "ms", 2500m, 1000m, 10000m);

    protected override RuleThreshold Limit => Max;
    public override string Code => "perf.cwv.lcp";
    public override string Name => "LCP yüksek";
    public override string Description => "Sayfa tipinin örnek sayfalarında Largest Contentful Paint (75. yüzdelik) Google eşiğini aşıyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "perf-doc:p30";
    public override string Impact => "Ana içerik geç görünür; kullanıcı sayfayı yavaş algılar ve Core Web Vitals değerlendirmesi zayıflar.";
    protected override string TitleTemplate => "{n} örnek sayfada LCP eşiği aşılıyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "En büyük içerik öğesini (genellikle ana görsel) erken ve küçük yükleyin.", "1. LCP öğesini belirleyin (genellikle ilk görünümdeki ana görsel).\n2. Bu görseli lazy yüklemeyin; preload ve fetchpriority=\"high\" kullanın.\n3. Görseli uygun boyutta ve modern biçimde (WebP/AVIF) sunun.\n4. Render'ı bloklayan CSS/JS'i azaltın ve TTFB'yi kontrol edin.");
    public override string TeamCriterion => "LCP görseli ve yükleme önceliği düzenlendi.";

    protected override double? Measure(VitalsSample sample) => sample.LcpMs;

    protected override string FormatValue(double value) => $"{value.ToString("N0", Tr)} ms";
}
