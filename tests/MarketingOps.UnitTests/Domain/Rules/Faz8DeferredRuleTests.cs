using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using static MarketingOps.UnitTests.Domain.Rules.RuleFixture;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Faz 8 ertelenen işler: canonical hedefi, yönlenene iç link, yetim sayfa, hreflang, X-Robots-Tag, render görünürlük doğrulaması.</summary>
public class Faz8DeferredRuleTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid CrawlId = Guid.CreateVersion7();

    private static PageSnapshotData HreflangData(params HreflangLink[] links) =>
        GoodMarkup with { Version = PageSnapshotData.SignalsVersion, Hreflangs = links };

    private static RenderSample VisibleSample(RenderMeasurements m, Guid? pageTypeId) =>
        RenderSample.Measured(CrawlId, Guid.CreateVersion7(), "https://example.com/sayfa", pageTypeId, RenderDevice.Desktop, m, false, T0);

    private static RuleContext WithRenders(params RenderSample[] samples) =>
        new(NewSite(), new Dictionary<string, decimal>(), new HashSet<string>(), [],
            ExternalData.CrawlOnly with { Available = new HashSet<RuleDataSource> { RuleDataSource.Crawl, RuleDataSource.Render }, Renders = samples });

    // ---- 1. seo.canonical.target ----

    [Fact]
    public void Canonical_target_is_checked_only_when_crawled_and_healthy_target_passes()
    {
        var source = Page(Good with { Canonical = "https://example.com/hedef" }, path: "/kaynak");
        var ok = Page(Good with { Canonical = "https://example.com/hedef" }, path: "/hedef");

        Run(new CanonicalTargetRule(), source, ok).ShouldBeEmpty();
        // Taranmamış hedef değerlendirilmez.
        Run(new CanonicalTargetRule(), source).ShouldBeEmpty();
    }

    [Fact]
    public void Canonical_target_reports_redirect_failure_noindex_and_chain()
    {
        var source = Page(Good with { Canonical = "https://example.com/hedef" }, path: "/kaynak");

        Run(new CanonicalTargetRule(), source, Page(chain: [new RedirectHop("https://example.com/hedef", 301)], path: "/hedef"))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Hedef yönleniyor");
        Run(new CanonicalTargetRule(), source, Page(PageSnapshotData.Empty, path: "/hedef", status: 404))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Hedef 404 dönüyor");
        Run(new CanonicalTargetRule(), source, Page(Good with { MetaRobots = "noindex" }, path: "/hedef"))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Hedef noindex");
        Run(new CanonicalTargetRule(), source, Page(Good with { Canonical = "https://example.com/baska" }, path: "/hedef"))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Hedefin canonical'ı başka adres");
    }

    // ---- 2. seo.links.to_redirect ----

    [Fact]
    public void Internal_links_to_redirecting_addresses_are_reported()
    {
        var page = Page(Good with { InternalLinks = ["https://example.com/eski", "https://example.com/yeni"] }, path: "/a");
        var redirect = Page(chain: [new RedirectHop("https://example.com/eski", 301)], path: "/eski");

        Run(new LinksToRedirectRule(), page, redirect).ShouldHaveSingleItem().CurrentValue.ShouldBe("https://example.com/eski");
        Run(new LinksToRedirectRule(), page).ShouldBeEmpty();
    }

    // ---- 3. seo.links.orphan ----

    [Fact]
    public void Sitemap_pages_without_inbound_links_are_orphans_except_the_home_page()
    {
        Run(new OrphanPageRule(), Page(path: "/yetim", inSitemap: true)).ShouldHaveSingleItem().Url.ShouldBe("https://example.com/yetim");

        var linking = Page(Good with { InternalLinks = ["https://example.com/yetim"] }, path: "/a");
        Run(new OrphanPageRule(), Page(path: "/yetim", inSitemap: true), linking).ShouldBeEmpty();
        Run(new OrphanPageRule(), Page(path: "/", inSitemap: true)).ShouldBeEmpty();
        Run(new OrphanPageRule(), Page(path: "/yetim", inSitemap: false)).ShouldBeEmpty();
    }

    [Fact]
    public void A_link_to_itself_does_not_rescue_an_orphan()
    {
        var selfLinking = Page(Good with { InternalLinks = ["https://example.com/yetim"] }, path: "/yetim", inSitemap: true);
        Run(new OrphanPageRule(), selfLinking).ShouldHaveSingleItem();
    }

    [Fact]
    public void A_link_to_an_old_address_reaches_the_redirect_target()
    {
        var target = Page(path: "/yeni", inSitemap: true);
        var oldAddress = Page(PageSnapshotData.Empty with { RedirectTarget = "https://example.com/yeni" }, path: "/eski", status: 301,
            chain: [new RedirectHop("https://example.com/eski", 301)]);
        var linking = Page(Good with { InternalLinks = ["https://example.com/eski"] }, path: "/a");

        Run(new OrphanPageRule(), target, oldAddress, linking).ShouldBeEmpty();
        Run(new OrphanPageRule(), target, oldAddress).ShouldHaveSingleItem().Url.ShouldBe("https://example.com/yeni");
    }

    [Fact]
    public void Orphan_rule_uses_the_full_crawl_data_source()
    {
        new OrphanPageRule().DataSource.ShouldBe(RuleDataSource.FullCrawl);
        new OrphanPageRule().DefaultSeverity.ShouldBe(Severity.Medium);
    }

    // ---- 4. hreflang ----

    [Fact]
    public void Hreflang_requires_valid_codes_a_unique_target_and_a_self_reference()
    {
        var valid = HreflangData(new HreflangLink("tr", "https://example.com/sayfa"), new HreflangLink("en", "https://example.com/en/page"));
        Run(new HreflangInvalidRule(), Page(valid)).ShouldBeEmpty();

        Run(new HreflangInvalidRule(), Page(HreflangData(new HreflangLink("turkish", "https://example.com/sayfa"))))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Geçersiz kod: turkish");
        Run(new HreflangInvalidRule(), Page(HreflangData(new HreflangLink("en", "https://example.com/en"), new HreflangLink("en", "https://example.com/en-us"))))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Aynı kod farklı adres: en");
        Run(new HreflangInvalidRule(), Page(HreflangData(new HreflangLink("en", "https://example.com/en/page"))))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Kendine referans yok");
    }

    [Fact]
    public void Hreflang_targets_must_link_back_and_be_healthy()
    {
        var source = HreflangData(new HreflangLink("tr", "https://example.com/sayfa"), new HreflangLink("en", "https://example.com/en/page"));
        var target = HreflangData(new HreflangLink("tr", "https://example.com/sayfa"), new HreflangLink("en", "https://example.com/en/page"));

        Run(new HreflangReturnMissingRule(), Page(source, path: "/sayfa"), Page(target, path: "/en/page")).ShouldBeEmpty();

        var oneWay = HreflangData(new HreflangLink("en", "https://example.com/en/page"));
        var violation = Run(new HreflangReturnMissingRule(), Page(source, path: "/sayfa"), Page(oneWay, path: "/en/page")).ShouldHaveSingleItem();
        violation.CurrentValue.ShouldNotBeNull().ShouldContain("geri bağlantı vermiyor");
    }

    [Fact]
    public void Hreflang_uncrawled_target_is_not_evaluated()
    {
        var source = HreflangData(new HreflangLink("en", "https://example.com/en/page"));
        Run(new HreflangReturnMissingRule(), Page(source, path: "/sayfa")).ShouldBeEmpty();
    }

    // ---- 5. seo.noindex.header ----

    [Fact]
    public void Noindex_only_in_the_robots_header_is_reported()
    {
        Run(new NoindexHeaderRule(), Page(HreflangData() with { RobotsHeader = "noindex, follow" })).ShouldHaveSingleItem().CurrentValue.ShouldBe("noindex, follow");
        Run(new NoindexHeaderRule(), Page(HreflangData() with { RobotsHeader = "none" })).ShouldHaveSingleItem();
        Run(new NoindexHeaderRule(), Page(HreflangData() with { RobotsHeader = "noindex", MetaRobots = "noindex" })).ShouldBeEmpty();
        Run(new NoindexHeaderRule(), Page(HreflangData() with { RobotsHeader = "index, follow" })).ShouldBeEmpty();
        Run(new NoindexHeaderRule(), Page(HreflangData())).ShouldBeEmpty();
    }

    [Fact]
    public void Robots_header_counts_as_noindex()
    {
        PageChecks.IsNoindex(Page(Good with { RobotsHeader = "noindex" })).ShouldBeTrue();
        PageChecks.IsNoindex(Page(Good with { RobotsHeader = "none" })).ShouldBeTrue();
        PageChecks.IsNoindex(Page(Good with { RobotsHeader = "index, follow" })).ShouldBeFalse();
        PageChecks.IsNoindex(Page(Good)).ShouldBeFalse();
        PageChecks.RobotsHeaderHasNoindex("googlebot: noindex").ShouldBeTrue();
        PageChecks.RobotsHeaderHasNoindex("bingbot: noindex").ShouldBeFalse();
        PageChecks.RobotsHeaderHasNoindex("unavailable_after: 25 Jun 2030 15:00:00 PST").ShouldBeFalse();
        PageChecks.HasSignals(Page(HreflangData())).ShouldBeTrue();
        PageChecks.HasSignals(Page(GoodMarkup)).ShouldBeFalse();
    }

    // ---- 8. render ile görünürlük doğrulaması ----

    [Fact]
    public void Html_link_and_form_rules_step_aside_when_render_sees_no_visible_offender()
    {
        var page = Page(GoodMarkup with { Links = new LinkFacts { NoText = Issue("a[href]") } }, pageTypeId: Product);
        var formPage = Page(GoodMarkup with { Forms = new FormFacts { Fields = 1, Unlabeled = Issue("input[name=q]") } }, pageTypeId: Product);

        // HTML'de sorun var, render verisi yok → bulgu açılır (eski davranış).
        Run(new LinkNoTextRule(), page).ShouldHaveSingleItem();
        Run(new FormLabelRule(), formPage).ShouldHaveSingleItem();

        // Render aynı tipte hiçbir görünür sorunlu öğe bulamadı → bulgu bastırılır.
        var clean = new RenderMeasurements { Version = RenderMeasurements.VisibilityVersion, VisibleNoTextLinks = 0, VisibleUnlabeledFields = 0 };
        Run(new LinkNoTextRule(), WithRenders(VisibleSample(clean, Product)), page).ShouldBeEmpty();
        Run(new FormLabelRule(), WithRenders(VisibleSample(clean, Product)), formPage).ShouldBeEmpty();

        // Render görünür sorunlu öğe buldu → bulgu açılır.
        var visible = clean with { VisibleNoTextLinks = 1, VisibleUnlabeledFields = 1 };
        Run(new LinkNoTextRule(), WithRenders(VisibleSample(visible, Product)), page).ShouldHaveSingleItem();
        Run(new FormLabelRule(), WithRenders(VisibleSample(visible, Product)), formPage).ShouldHaveSingleItem();

        // Eski ölçüm (sürüm 0) görünürlük sayımı taşımaz → bastırma yapılmaz.
        var old = new RenderMeasurements { VisibleNoTextLinks = 0, VisibleUnlabeledFields = 0 };
        Run(new LinkNoTextRule(), WithRenders(VisibleSample(old, Product)), page).ShouldHaveSingleItem();
    }
}
