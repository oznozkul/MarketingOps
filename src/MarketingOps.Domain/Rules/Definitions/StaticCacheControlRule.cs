using System.Globalization;
using System.Text.RegularExpressions;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Taramada örneklenen CSS/JS dosyalarının Cache-Control süresini değerlendirir. Sayfaya değil siteye aittir.</summary>
public sealed partial class StaticCacheControlRule : RuleBase, ISiteRule
{
    public static readonly RuleThreshold MinDays = new("minDays", "En kısa önbellek süresi", "gün", 30m, 1m, 365m);

    public override IReadOnlyList<RuleThreshold> Thresholds => [MinDays];
    public override string Code => "perf.cache_control.static";
    public override string Name => "Statik dosyalarda Cache-Control eksik ya da kısa";
    public override string Description => "CSS ve JS dosyalarının Cache-Control max-age süresini kontrol eder.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.Medium;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "perf-doc:p34";
    public override string Impact => "Önbellek süresi kısa olan dosyalar her ziyarette yeniden indirilir; tekrar eden ziyaretler yavaşlar.";
    protected override string TitleTemplate => "{n} statik dosyada önbellek süresi kısa ya da yok";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Code, "Sürümlü (adında hash olan) statik dosyalar uzun süre önbelleğe alınmalı.", "Cache-Control: public, max-age=31536000, immutable");
    public override string TeamCriterion => "Statik dosyalar için önbellek başlığı sunucu/CDN'de tanımlandı.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code, context);

    private sealed class Run(string ruleCode, RuleContext context) : ISiteRuleRun
    {
        public void Observe(PageSnapshot page) { }

        public IEnumerable<RuleViolation> Complete()
        {
            var minSeconds = context.Threshold(MinDays) * 86400m;
            foreach (var asset in context.Assets.Where(a => a.StatusCode is >= 200 and < 300))
            {
                var cacheControl = asset.CacheControl;
                var maxAge = MaxAge(cacheControl);
                var noStore = cacheControl?.Contains("no-store", StringComparison.OrdinalIgnoreCase) == true;
                if (maxAge is null || maxAge < minSeconds || noStore)
                    yield return new RuleViolation(ruleCode, null, asset.Url, cacheControl ?? "Cache-Control yok");
            }
        }
    }

    private static decimal? MaxAge(string? cacheControl) =>
        cacheControl is not null && MaxAgeRegex().Match(cacheControl) is { Success: true } m
            ? decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture)
            : null;

    [GeneratedRegex(@"max-age\s*=\s*(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex MaxAgeRegex();
}
