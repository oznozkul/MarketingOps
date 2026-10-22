using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Domain.Sites;
using static MarketingOps.UnitTests.Domain.Rules.RuleFixture;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Her kural için en az bir geçen ve bir kalan senaryo.</summary>
public class RuleTests
{
    [Fact]
    public void Clean_page_violates_no_page_rule()
    {
        foreach (var rule in RuleCatalog.All.OfType<IPageRule>())
            Run(rule, Page()).ShouldBeEmpty(rule.Code);
    }

    [Fact]
    public void Catalog_codes_are_unique() =>
        RuleCatalog.All.Select(r => r.Code).ShouldBeUnique();

    [Fact]
    public void Title_missing()
    {
        Run(new TitleMissingRule(), Page(Good with { Title = " " })).ShouldHaveSingleItem();
        Run(new TitleMissingRule(), Page(Good with { Title = null, MetaRobots = "noindex" })).ShouldBeEmpty();
    }

    [Fact]
    public void Title_duplicate_groups_pages_with_the_same_title()
    {
        var violations = Run(new TitleDuplicateRule(), Page(path: "/a"), Page(path: "/b"), Page(Good with { Title = "Başka" }, path: "/c"));
        violations.Select(v => v.Url).ShouldBe(["https://example.com/a", "https://example.com/b"], ignoreOrder: true);
        Run(new TitleDuplicateRule(), Page(path: "/a"), Page(Good with { Title = "Başka" }, path: "/b")).ShouldBeEmpty();
    }

    [Fact]
    public void Title_length_uses_thresholds()
    {
        Run(new TitleLengthRule(), Page(Good with { Title = "Kısa" })).ShouldHaveSingleItem().CurrentValue.ShouldBe("4 karakter");
        Run(new TitleLengthRule(), Context(thresholds: new Dictionary<string, decimal> { ["minLength"] = 3 }), Page(Good with { Title = "Kısa" })).ShouldBeEmpty();
    }

    [Fact]
    public void Meta_description_missing()
    {
        Run(new MetaDescriptionMissingRule(), Page(Good with { MetaDescription = null })).ShouldHaveSingleItem();
        Run(new MetaDescriptionMissingRule(), Page()).ShouldBeEmpty();
    }

    [Fact]
    public void Meta_description_duplicate()
    {
        Run(new MetaDescriptionDuplicateRule(), Page(path: "/a"), Page(path: "/b")).Count.ShouldBe(2);
        Run(new MetaDescriptionDuplicateRule(), Page(path: "/a"), Page(Good with { MetaDescription = "Farklı" }, path: "/b")).ShouldBeEmpty();
    }

    [Fact]
    public void Heading_missing_or_multiple_h1()
    {
        Run(new HeadingRule(), Page(Good with { H1 = [] })).ShouldHaveSingleItem().CurrentValue.ShouldBe("H1 yok");
        Run(new HeadingRule(), Page(Good with { H1 = ["A", "B"] })).ShouldHaveSingleItem().CurrentValue.ShouldBe("2 H1");
    }

    [Fact]
    public void Canonical_missing_or_pointing_to_another_domain()
    {
        Run(new CanonicalRule(), Page(Good with { Canonical = null })).ShouldHaveSingleItem();
        Run(new CanonicalRule(), Page(Good with { Canonical = "https://baska.com/sayfa" })).ShouldHaveSingleItem();
        Run(new CanonicalRule(), Page(Good with { Canonical = "https://www.example.com/sayfa" })).ShouldBeEmpty();
    }

    [Fact]
    public void Noindex_page_listed_in_sitemap()
    {
        Run(new NoindexInSitemapRule(), Page(Good with { MetaRobots = "noindex, follow" }, inSitemap: true)).ShouldHaveSingleItem();
        Run(new NoindexInSitemapRule(), Page(Good with { MetaRobots = "noindex" }, inSitemap: false)).ShouldBeEmpty();
    }

    [Fact]
    public void Broken_internal_links_point_to_failed_pages()
    {
        var page = Page(Good with { InternalLinks = ["https://example.com/yok", "https://example.com/var"] });
        var context = Context(failedUrls: new HashSet<string> { "https://example.com/yok" });
        Run(new BrokenInternalLinksRule(), context, page).ShouldHaveSingleItem().CurrentValue.ShouldBe("https://example.com/yok");
        Run(new BrokenInternalLinksRule(), page).ShouldBeEmpty();
    }

    [Fact]
    public void Redirect_chain_with_three_or_more_hops()
    {
        RedirectHop Hop(int i) => new($"https://example.com/r{i}", 301);
        Run(new RedirectChainRule(), Page(chain: [Hop(1), Hop(2), Hop(3)])).ShouldHaveSingleItem();
        Run(new RedirectChainRule(), Page(chain: [Hop(1), Hop(2)])).ShouldBeEmpty();
    }

    [Fact]
    public void Image_alt_missing_ignores_empty_alt()
    {
        Run(new ImageAltMissingRule(), Page(Good with { Images = [new ImageInfo("/a.jpg", null, "10", "10", null)] })).ShouldHaveSingleItem();
        Run(new ImageAltMissingRule(), Page(Good with { Images = [new ImageInfo("/a.jpg", "", "10", "10", null)] })).ShouldBeEmpty();
    }

    [Fact]
    public void Required_schema_comes_from_the_page_type_conditions()
    {
        var site = NewSite();
        var product = site.AddPageType("Ürün", MatchMode.All, [new MatchCondition(MatchConditionType.UrlGlob, "/urun/*"), new MatchCondition(MatchConditionType.SchemaType, "Product")]);
        var context = Context(site);
        Run(new RequiredSchemaRule(), context, Page(pageTypeId: product.Id)).ShouldHaveSingleItem().Detail.ShouldBe("Beklenen: Product");
        Run(new RequiredSchemaRule(), context, Page(Good with { SchemaTypes = ["Product"] }, pageTypeId: product.Id)).ShouldBeEmpty();
    }

    [Fact]
    public void Thin_content_below_word_threshold()
    {
        Run(new ThinContentRule(), Page(Good with { WordCount = 80 })).ShouldHaveSingleItem();
        Run(new ThinContentRule(), Context(thresholds: new Dictionary<string, decimal> { ["minWords"] = 50 }), Page(Good with { WordCount = 80 })).ShouldBeEmpty();
    }

    [Fact]
    public void Compression_missing_on_large_html()
    {
        Run(new CompressionMissingRule(), Page(encoding: null)).ShouldHaveSingleItem();
        Run(new CompressionMissingRule(), Page(encoding: "br")).ShouldBeEmpty();
        Run(new CompressionMissingRule(), Page(encoding: null, sizeBytes: 500)).ShouldBeEmpty();
    }

    [Fact]
    public void Static_cache_control_checks_sampled_assets()
    {
        var context = Context(assets:
        [
            new AssetCheck("https://example.com/app.js", 200, "text/javascript", "max-age=60", "gzip"),
            new AssetCheck("https://example.com/app.css", 200, "text/css", "public, max-age=31536000", "gzip"),
            new AssetCheck("https://example.com/eski.js", 404, null, null, null)
        ]);
        Run(new StaticCacheControlRule(), context).ShouldHaveSingleItem().Url.ShouldBe("https://example.com/app.js");
    }

    [Fact]
    public void Render_blocking_script_in_head()
    {
        Run(new RenderBlockingScriptRule(), Page(Good with { Scripts = [new ScriptInfo("https://example.com/a.js", false, false, true)] })).ShouldHaveSingleItem();
        Run(new RenderBlockingScriptRule(), Page(Good with { Scripts = [new ScriptInfo("https://example.com/a.js", false, true, true)] })).ShouldBeEmpty();
    }

    [Fact]
    public void Image_dimensions_missing()
    {
        Run(new ImageDimensionsRule(), Page(Good with { Images = [new ImageInfo("/a.jpg", "a", null, "10", null)] })).ShouldHaveSingleItem();
        Run(new ImageDimensionsRule(), Page(Good with { Images = [new ImageInfo("/a.jpg", "a", "10", "10", null)] })).ShouldBeEmpty();
    }

    [Fact]
    public void Lazy_load_missing_below_the_fold()
    {
        ImageInfo Image(int i, string? loading) => new($"/g{i}.jpg", "g", "10", "10", loading);
        Run(new LazyLoadMissingRule(), Page(Good with { Images = Enumerable.Range(1, 5).Select(i => Image(i, null)).ToList() }))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("2 görsel");
        Run(new LazyLoadMissingRule(), Page(Good with { Images = Enumerable.Range(1, 5).Select(i => Image(i, i > 3 ? "lazy" : null)).ToList() })).ShouldBeEmpty();
    }

    [Fact]
    public void Preconnect_missing_for_third_party_origin_or_too_many()
    {
        var withCdn = Good with { Stylesheets = ["https://cdn.example.net/a.css"] };
        Run(new PreconnectRule(), Page(withCdn)).ShouldHaveSingleItem();
        Run(new PreconnectRule(), Page(withCdn with { Preconnects = ["https://cdn.example.net"] })).ShouldBeEmpty();
        var many = Enumerable.Range(1, 5).Select(i => $"https://c{i}.example.net").ToList();
        Run(new PreconnectRule(), Page(Good with { Preconnects = many })).ShouldHaveSingleItem().CurrentValue.ShouldBe("5 preconnect");
    }

    [Fact]
    public void Html_size_above_threshold()
    {
        Run(new HtmlSizeRule(), Page(sizeBytes: 600 * 1024)).ShouldHaveSingleItem();
        Run(new HtmlSizeRule(), Page(sizeBytes: 100 * 1024)).ShouldBeEmpty();
    }

    [Fact]
    public void Ttfb_is_judged_by_page_type_average()
    {
        var slowType = Guid.CreateVersion7();
        var violations = Run(new TtfbRule(), Page(path: "/a", pageTypeId: slowType, ttfbMs: 1500), Page(path: "/b", pageTypeId: slowType, ttfbMs: 700),
            Page(path: "/c", ttfbMs: 2000), Page(path: "/d", ttfbMs: 100), Page(path: "/e", ttfbMs: 100));
        // Yavaş tipin ortalaması 1100 ms: yalnızca eşiği aşan sayfa raporlanır. Eşleşmeyenlerin ortalaması 733 ms: bulgu yok.
        violations.ShouldHaveSingleItem().Url.ShouldBe("https://example.com/a");
    }

    [Fact]
    public void Non_html_and_failed_pages_are_ignored_by_content_rules()
    {
        var failed = Page(PageSnapshotData.Empty, status: 404);
        foreach (var rule in new IPageRule[] { new TitleMissingRule(), new HeadingRule(), new CanonicalRule(), new ThinContentRule(), new MetaDescriptionMissingRule() })
            Run(rule, failed).ShouldBeEmpty(rule.Code);
    }
}
