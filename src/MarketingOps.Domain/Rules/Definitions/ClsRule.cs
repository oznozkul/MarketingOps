using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class ClsRule : WebVitalRule
{
    public static readonly RuleThreshold Max = new("max", "En yüksek CLS", "puan", 0.1m, 0.01m, 1m);

    protected override RuleThreshold Limit => Max;
    public override string Code => "perf.cwv.cls";
    public override string Name => "CLS yüksek";
    public override string Description => "Sayfa tipinin örnek sayfalarında Cumulative Layout Shift (75. yüzdelik) Google eşiğini aşıyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "perf-doc:p26";
    public override string Impact => "Sayfa yüklenirken içerik kayar; kullanıcı yanlış öğeye tıklayabilir.";
    protected override string TitleTemplate => "{n} örnek sayfada CLS eşiği aşılıyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Yüklenen öğeler için önceden yer ayırın.", "1. Görsellere ve iframe'lere width/height verin.\n2. Reklam, banner ve sonradan eklenen bileşenler için sabit alan ayırın.\n3. Web fontlarında font-display ve boyut uyumlu yedek font kullanın.");
    public override string TeamCriterion => "Kaymaya neden olan öğeler için alan ayrıldı.";

    protected override double? Measure(VitalsSample sample) => sample.Cls;

    protected override string FormatValue(double value) => value.ToString("0.00", Tr);
}
