using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Metrics;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Core Web Vitals eşik kurallarının ortak değerlendirmesi: sayfa tipinin örnek sayfalarından eşiği aşanlar.
/// Bulgu sayfa tipi başına tektir; mobil ve masaüstü ölçümleri ayrı satır olarak listelenir.
/// </summary>
public abstract class WebVitalRule : RuleBase, ISiteRule
{
    public override RuleDataSource DataSource => RuleDataSource.PageSpeed;
    public override RuleScope Scope => RuleScope.Site;

    protected abstract RuleThreshold Limit { get; }

    protected abstract double? Measure(VitalsSample sample);

    protected abstract string FormatValue(double value);

    public override IReadOnlyList<RuleThreshold> Thresholds => [Limit];

    public ISiteRuleRun Start(RuleContext context) => new Run(this, context);

    private sealed class Run(WebVitalRule rule, RuleContext context) : ISiteRuleRun
    {
        public void Observe(PageSnapshot page) { }

        public IEnumerable<RuleViolation> Complete()
        {
            var limit = (double)context.Threshold(rule.Limit);
            foreach (var sample in context.External.Vitals)
            {
                if (rule.Measure(sample) is not { } value || value <= limit) continue;
                var device = sample.Strategy == DeviceStrategy.Mobile ? "mobil" : "masaüstü";
                yield return new RuleViolation(rule.Code, sample.PageTypeId, sample.Url, $"{rule.FormatValue(value)} ({device})",
                    $"Eşik {rule.FormatValue(limit)}");
            }
        }
    }
}
