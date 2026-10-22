using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Sayfa tipi bazında ortalama TTFB. Ortalaması eşiği aşan tiplerde, eşiği aşan sayfalar etkilenen sayfadır.</summary>
public sealed class TtfbRule : RuleBase, ISiteRule
{
    public static readonly RuleThreshold MaxMs = new("maxMs", "En yüksek ortalama TTFB", "ms", 800m, 100m, 10000m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MaxMs];
    public override string Code => "perf.ttfb.high";
    public override string Name => "TTFB yüksek";
    public override string Description => "Sayfa tipi bazında sunucunun ilk bayta kadar geçen yanıt süresini (TTFB) ölçer.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "perf-doc:p30";
    public override string Impact => "TTFB, sayfanın görünür hale gelme süresinin alt sınırını belirler. Sunucu tarafındaki sebep dışarıdan görülemez; öneriler kontrol edilecek hipotezlerdir.";
    protected override string TitleTemplate => "{n} sayfada TTFB yüksek";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Sunucunun ilk bayta yanıt süresi kısaltılmalı.", "1. Sayfa tipinin HTML'ini sayfa önbelleği ya da CDN ile önbelleğe alın.\n2. Sayfa üretimindeki yavaş veritabanı sorgularını ve dış servis çağrılarını tespit edip iyileştirin.\n3. Sunucu kaynaklarını ve bölgesini (kullanıcılara yakın) kontrol edin.");
    public override string TeamCriterion => "Sayfa tipi için önbellek/sorgu iyileştirmesi canlıya alındı.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code, context.Threshold(MaxMs));

    private sealed class Run(string ruleCode, decimal maxMs) : ISiteRuleRun
    {
        // Sayfa tipine uymayanlar da ayrı bir grup olarak değerlendirilir.
        private readonly Dictionary<Guid, List<(string Url, int Ms)>> _byType = [];

        public void Observe(PageSnapshot page)
        {
            if (!PageChecks.IsHtml(page) || page.ResponseTimeMs is not { } ms) return;
            var key = page.PageTypeId ?? Guid.Empty;
            if (!_byType.TryGetValue(key, out var pages)) _byType[key] = pages = [];
            pages.Add((page.Url, ms));
        }

        public IEnumerable<RuleViolation> Complete()
        {
            foreach (var (key, pages) in _byType)
            {
                var average = pages.Average(p => p.Ms);
                if ((decimal)average <= maxMs) continue;
                var pageTypeId = key == Guid.Empty ? (Guid?)null : key;
                foreach (var (url, ms) in pages.Where(p => p.Ms > maxMs))
                    yield return new RuleViolation(ruleCode, pageTypeId, url, $"{ms.ToString("N0", Tr)} ms", $"Sayfa tipi ortalaması {average.ToString("N0", Tr)} ms");
            }
        }
    }
}
