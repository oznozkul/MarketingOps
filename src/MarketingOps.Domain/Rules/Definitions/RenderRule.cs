using System.Globalization;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rendering;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>
/// Render denetimi kurallarının ortak değerlendirmesi: taramanın örnek sayfalarında, kuralın baktığı cihazlardaki sorunlu öğeler.
/// Bir sayfa birden çok cihazda sorunluysa tek ihlaldir; cihazlar ve en fazla beş örnek öğe ayrıntıda yazar.
/// Bulgu, diğer kurallar gibi sayfa tipi başına tektir.
/// </summary>
public abstract class RenderRule : RuleBase, ISiteRule
{
    private const int MaxSamples = 5;
    protected static readonly CultureInfo Number = CultureInfo.GetCultureInfo("tr-TR");

    public override RuleDataSource DataSource => RuleDataSource.Render;
    public override RuleScope Scope => RuleScope.Site;

    /// <summary>Kuralın baktığı cihazlar; dokunma alanı ve taşma yalnızca mobilde anlamlıdır.</summary>
    protected virtual IReadOnlyCollection<RenderDevice> Devices => [RenderDevice.Desktop, RenderDevice.Tablet, RenderDevice.Mobile];

    /// <summary>Ekranı bir katman kaplıyorsa (çerez penceresi) sonuç o katmanı ölçer; ilk ekran ve kontrast kuralları o örneği atlar.</summary>
    protected virtual bool NeedsClearView => false;

    protected abstract IReadOnlyList<RenderElement> Offenders(RenderMeasurements measurements, RuleContext context);

    protected abstract string Describe(RenderElement element);

    /// <summary>Bulgunun "mevcut değer" alanı; varsayılanı en çok sorun görülen cihazdaki öğe sayısı. Cihazlar ayrı alandadır.</summary>
    protected virtual string Current(IReadOnlyList<(RenderSample Sample, IReadOnlyList<RenderElement> Offenders)> hits) =>
        $"{hits.Max(h => h.Offenders.Count).ToString("N0", Number)} öğe";

    public ISiteRuleRun Start(RuleContext context) => new Run(this, context);

    protected static string DeviceNames(IEnumerable<(RenderSample Sample, IReadOnlyList<RenderElement> Offenders)> hits) =>
        string.Join(", ", hits.Select(h => h.Sample.Device).Distinct().Order().Select(DeviceName));

    protected static string DeviceName(RenderDevice device) => device switch
    {
        RenderDevice.Desktop => "masaüstü",
        RenderDevice.Tablet => "tablet",
        _ => "mobil"
    };

    protected static string Quote(RenderElement element) =>
        string.IsNullOrWhiteSpace(element.Text) ? element.Selector : $"{element.Selector} \"{element.Text}\"";

    private sealed class Run(RenderRule rule, RuleContext context) : ISiteRuleRun
    {
        public void Observe(PageSnapshot page) { }

        public IEnumerable<RuleViolation> Complete()
        {
            var samples = context.External.Renders
                .Where(s => s.HasMeasurements && (!rule.NeedsClearView || s.HasClearView) && rule.Devices.Contains(s.Device));
            foreach (var page in samples.GroupBy(s => s.Url))
            {
                var hits = page
                    .Select(s => (Sample: s, Offenders: rule.Offenders(s.Measurements, context)))
                    .Where(h => h.Offenders.Count > 0)
                    .ToList();
                if (hits.Count == 0) continue;
                var examples = hits.SelectMany(h => h.Offenders).DistinctBy(e => e.Selector).Take(MaxSamples).Select(rule.Describe);
                yield return new RuleViolation(rule.Code, page.First().PageTypeId, page.Key, rule.Current(hits), string.Join(" · ", examples), DeviceNames(hits));
            }
        }
    }
}
