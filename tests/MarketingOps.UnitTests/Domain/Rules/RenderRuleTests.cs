using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Domain.Metrics;
using static MarketingOps.UnitTests.Domain.Rules.RuleFixture;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Faz 8 render kuralları: örnekler sayfa başına tek ihlal, cihazlar birleşir, engellenmiş ölçüm ilk ekran/kontrastta kullanılmaz.</summary>
public class RenderRuleTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid CrawlId = Guid.CreateVersion7();

    private static RenderSample Sample(RenderDevice device, RenderMeasurements m, string path = "/urun/1", bool obstructed = false) =>
        RenderSample.Measured(CrawlId, Guid.CreateVersion7(), $"https://example.com{path}", Product, device, m, obstructed, T0);

    private static RuleContext With(IReadOnlyDictionary<string, decimal>? thresholds = null, params RenderSample[] samples) =>
        new(NewSite(), thresholds ?? new Dictionary<string, decimal>(), new HashSet<string>(), [],
            ExternalData.CrawlOnly with { Available = new HashSet<RuleDataSource> { RuleDataSource.Crawl, RuleDataSource.Render }, Renders = samples });

    private static RenderElement El(string selector, double value, string? text = null, double? limit = null) => new(selector, text, value, limit);

    [Fact]
    public void Render_rules_read_the_render_source()
    {
        RuleCatalog.All.OfType<RenderRule>().Count().ShouldBe(8);
        RuleCatalog.All.OfType<RenderRule>().ShouldAllBe(r => r.DataSource == RuleDataSource.Render);
    }

    [Fact]
    public void Mobile_overflow_reports_viewport_and_document_width()
    {
        var overflowing = new RenderMeasurements { ViewportWidth = 375, DocumentWidth = 412, OverflowOffenders = [El("div.hero > img", 412)] };

        var violation = Run(new MobileOverflowRule(), With(samples: Sample(RenderDevice.Mobile, overflowing))).ShouldHaveSingleItem();
        violation.CurrentValue.ShouldBe("375 px'de 412 px");
        violation.Detail.ShouldBe("div.hero > img (412 px)");
        violation.PageTypeId.ShouldBe(Product);
    }

    [Fact]
    public void Mobile_overflow_tolerates_a_few_pixels_and_ignores_desktop()
    {
        var nearly = new RenderMeasurements { ViewportWidth = 375, DocumentWidth = 379 };
        var desktop = new RenderMeasurements { ViewportWidth = 1366, DocumentWidth = 1500 };

        Run(new MobileOverflowRule(), With(samples: Sample(RenderDevice.Mobile, nearly))).ShouldBeEmpty();
        Run(new MobileOverflowRule(), With(samples: Sample(RenderDevice.Desktop, desktop))).ShouldBeEmpty();
    }

    [Fact]
    public void One_page_failing_on_several_devices_is_a_single_violation_naming_them()
    {
        var bad = new RenderMeasurements { ContrastFailures = [El("p.fiyat", 2.9, "1.299 TL", 4.5)] };

        var violation = Run(new ContrastRule(), With(samples: [Sample(RenderDevice.Mobile, bad), Sample(RenderDevice.Desktop, bad)])).ShouldHaveSingleItem();
        violation.CurrentValue.ShouldBe("1 öğe");
        violation.Devices.ShouldBe("masaüstü, mobil");
        violation.Detail.ShouldBe("p.fiyat \"1.299 TL\" 2,9:1 (gereken 4,5:1)");
    }

    [Fact]
    public void Obstructed_samples_are_skipped_for_contrast_and_fold_but_count_for_text_size()
    {
        var m = new RenderMeasurements { ContrastFailures = [El("p", 2, null, 4.5)], LazyAboveFold = [El("img.banner", 0)], SmallText = [El("small", 10)] };
        var sample = Sample(RenderDevice.Desktop, m, obstructed: true);

        Run(new ContrastRule(), With(samples: sample)).ShouldBeEmpty();
        Run(new LazyAboveFoldRule(), With(samples: sample)).ShouldBeEmpty();
        Run(new SmallTextRule(), With(samples: sample)).ShouldHaveSingleItem();
    }

    [Fact]
    public void Failed_samples_are_never_evaluated()
    {
        var failed = RenderSample.Failed(CrawlId, Guid.CreateVersion7(), "https://example.com/urun/1", Product, RenderDevice.Mobile, "Zaman aşımı.", T0);

        foreach (var rule in RuleCatalog.All.OfType<RenderRule>()) Run(rule, With(samples: failed)).ShouldBeEmpty(rule.Code);
    }

    [Fact]
    public void Small_text_and_tap_targets_use_site_thresholds()
    {
        var text = new RenderMeasurements { SmallText = [El("small", 11), El("p.not", 13)] };
        Run(new SmallTextRule(), With(samples: Sample(RenderDevice.Desktop, text))).ShouldHaveSingleItem().CurrentValue.ShouldBe("1 öğe");
        Run(new SmallTextRule(), With(new Dictionary<string, decimal> { ["minPx"] = 14 }, Sample(RenderDevice.Desktop, text)))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("2 öğe");

        var taps = new RenderMeasurements { SmallTapTargets = [El("a.favori", 20), El("button.sepet", 40)] };
        Run(new TapTargetRule(), With(samples: Sample(RenderDevice.Mobile, taps))).ShouldHaveSingleItem().Detail.ShouldBe("a.favori (20 px)");
        Run(new TapTargetRule(), With(new Dictionary<string, decimal> { ["minPx"] = 44 }, Sample(RenderDevice.Mobile, taps)))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("2 öğe");
        Run(new TapTargetRule(), With(samples: Sample(RenderDevice.Tablet, taps))).ShouldBeEmpty();
    }

    [Fact]
    public void Wide_elements_fold_and_distortion_rules_report_their_lists()
    {
        var m = new RenderMeasurements
        {
            WideElements = [El("table.ebatlar", 640)],
            LazyAboveFold = [El("img.banner", 0, "kis.jpg")],
            EagerBelowFold = [El("img.urun", 0, "u1.jpg"), El("img.urun2", 0, "u2.jpg")],
            DistortedImages = [El("img.logo", 18, "logo.png")]
        };
        var samples = new[] { Sample(RenderDevice.Mobile, m) };

        Run(new WideElementRule(), With(samples: samples)).ShouldHaveSingleItem().Detail.ShouldBe("table.ebatlar (640 px)");
        Run(new LazyAboveFoldRule(), With(samples: samples)).ShouldHaveSingleItem().Detail.ShouldBe("img.banner \"kis.jpg\"");
        Run(new EagerBelowFoldRule(), With(samples: samples)).ShouldHaveSingleItem().CurrentValue.ShouldBe("2 öğe");
        Run(new DistortedImageRule(), With(samples: samples)).ShouldHaveSingleItem().Detail.ShouldBe("img.logo \"logo.png\" (%18 sapma)");
    }

    [Fact]
    public void Each_sampled_page_is_its_own_violation()
    {
        var bad = new RenderMeasurements { DistortedImages = [El("img", 10)] };

        Run(new DistortedImageRule(), With(samples: [Sample(RenderDevice.Mobile, bad, "/urun/1"), Sample(RenderDevice.Mobile, bad, "/urun/2")]))
            .Select(v => v.Url).ShouldBe(["https://example.com/urun/1", "https://example.com/urun/2"]);
    }

    [Fact]
    public void Lazy_heuristic_steps_aside_for_page_types_measured_in_render()
    {
        var images = Enumerable.Range(1, 5).Select(i => new MarketingOps.Domain.Crawls.ImageInfo($"/g{i}.jpg", "g", "1", "1", null)).ToList();
        var page = Page(Good with { Images = images }, pageTypeId: Product);

        Run(new LazyLoadMissingRule(), page).ShouldHaveSingleItem();
        Run(new LazyLoadMissingRule(), With(samples: Sample(RenderDevice.Mobile, RenderMeasurements.Empty)), page).ShouldBeEmpty();
        Run(new LazyLoadMissingRule(), With(samples: Sample(RenderDevice.Mobile, RenderMeasurements.Empty, obstructed: true)), page).ShouldHaveSingleItem();
    }
}
