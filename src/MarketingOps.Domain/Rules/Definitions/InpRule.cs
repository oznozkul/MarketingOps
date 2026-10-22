using MarketingOps.Domain.Findings;
using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

public sealed class InpRule : WebVitalRule
{
    public static readonly RuleThreshold Max = new("maxMs", "En yüksek INP", "ms", 200m, 50m, 2000m);

    protected override RuleThreshold Limit => Max;
    public override string Code => "perf.cwv.inp";
    public override string Name => "INP yüksek";
    public override string Description => "Sayfa tipinin örnek sayfalarında Interaction to Next Paint (75. yüzdelik) Google eşiğini aşıyor mu kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override string ReferenceKey => "perf-doc:p22";
    public override string Impact => "Tıklama ve dokunmalara geç yanıt verilir; özellikle filtre, sepet ve ödeme adımlarında kullanıcı deneyimi bozulur.";
    protected override string TitleTemplate => "{n} örnek sayfada INP eşiği aşılıyor";
    public override Team Team => Team.Frontend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Etkileşimde çalışan uzun JavaScript görevlerini kısaltın.", "1. Etkileşim sırasında çalışan uzun görevleri tarayıcı performans aracıyla bulun.\n2. Üçüncü taraf scriptleri erteleyin ya da azaltın.\n3. Ağır işleri parçalara bölün ve etkileşimden sonraya bırakın.");
    public override string TeamCriterion => "Etkileşimde çalışan uzun görevler azaltıldı.";

    protected override double? Measure(VitalsSample sample) => sample.InpMs;

    protected override string FormatValue(double value) => $"{value.ToString("N0", Tr)} ms";
}
