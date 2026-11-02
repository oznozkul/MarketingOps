using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using static MarketingOps.UnitTests.Domain.Rules.RuleFixture;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Faz 8 HTML kuralları: ihlal, temiz sayfa, sınır ve eski sürüm snapshot senaryoları.</summary>
public class MarkupRuleTests
{
    private static StructuredDataInfo Schema(params SchemaNode[] nodes) => new() { JsonLdBlocks = 1, Nodes = nodes };

    private static SchemaNode Node(string type, params string[] properties) => new() { Type = type, TopLevel = true, Properties = properties };

    [Fact]
    public void Markup_rules_ignore_snapshots_from_before_the_markup_parser()
    {
        var old = Good with { StructuredData = StructuredDataInfo.Empty, Links = new LinkFacts { NoText = Issue("a") } };
        var markupRules = RuleCatalog.All.OfType<IPageRule>().Where(r => Run(r, Page(GoodMarkup with { Links = old.Links, StructuredData = old.StructuredData })).Count > 0);

        foreach (var rule in markupRules) Run(rule, Page(old)).ShouldBeEmpty(rule.Code);
    }

    [Fact]
    public void Accessibility_rules_cover_noindex_pages_but_seo_rules_do_not()
    {
        var noindex = GoodMarkup with { MetaRobots = "noindex", Forms = new FormFacts { Fields = 1, Unlabeled = Issue("input[name=\"q\"]") }, StructuredData = StructuredDataInfo.Empty };

        Run(new FormLabelRule(), Page(noindex)).ShouldHaveSingleItem();
        Run(new StructuredDataMissingRule(), Page(noindex)).ShouldBeEmpty();
    }

    [Fact]
    public void Structured_data_is_required_on_every_indexable_page()
    {
        Run(new StructuredDataMissingRule(), Page(GoodMarkup with { StructuredData = StructuredDataInfo.Empty }))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Structured data yok");
        Run(new StructuredDataMissingRule(), Page(GoodMarkup with { StructuredData = new StructuredDataInfo { MicrodataTypes = ["Product"] } })).ShouldBeEmpty();
        Run(new StructuredDataMissingRule(), Page(GoodMarkup with { StructuredData = new StructuredDataInfo { Invalid = Issue("{ bozuk") } }))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Yalnızca bozuk JSON-LD var");
    }

    [Fact]
    public void Broken_json_ld_is_reported_with_its_beginning()
    {
        var violation = Run(new StructuredDataInvalidRule(), Page(GoodMarkup with { StructuredData = GoodMarkup.StructuredData with { Invalid = Issue("{ \"@type\": \"Product\"") } }))
            .ShouldHaveSingleItem();

        violation.CurrentValue.ShouldBe("1 bozuk blok");
        violation.Detail.ShouldBe("{ \"@type\": \"Product\"");
    }

    [Fact]
    public void Product_requires_name_image_and_an_offer_or_review()
    {
        SchemaRequirements.Missing(Node("Product", "name")).ShouldBe(["image", "offers | review | aggregateRating"]);
        SchemaRequirements.Missing(Node("Product", "name", "image", "aggregateRating")).ShouldBeEmpty();
    }

    [Fact]
    public void An_offer_needs_price_currency_and_availability_and_an_aggregate_offer_its_low_price()
    {
        SchemaRequirements.Missing(Node("Product", "name", "image", "offers", "offers.price", "offers.priceCurrency"))
            .ShouldBe(["offers.availability"]);
        SchemaRequirements.Missing(Node("Product", "name", "image", "offers", "offers.priceSpecification.price", "offers.priceSpecification.priceCurrency", "offers.availability"))
            .ShouldBeEmpty();
        SchemaRequirements.Missing(Node("Product", "name", "image", "offers", "offers.lowPrice", "offers.priceCurrency")).ShouldBeEmpty();
    }

    [Fact]
    public void Breadcrumb_item_name_may_sit_in_the_list_item_or_the_item_it_points_to()
    {
        SchemaRequirements.Missing(Node("BreadcrumbList", "itemListElement", "itemListElement.position", "itemListElement.item", "itemListElement.item.name"))
            .ShouldBeEmpty();
        SchemaRequirements.Missing(Node("BreadcrumbList", "itemListElement", "itemListElement.position", "itemListElement.item"))
            .ShouldBe(["itemListElement.name | itemListElement.item.name"]);
    }

    [Fact]
    public void The_same_gap_in_repeated_blocks_is_reported_once()
    {
        var crumbs = Node("BreadcrumbList", "itemListElement", "itemListElement.position");
        Run(new StructuredDataPropertiesRule(), Page(GoodMarkup with { StructuredData = Schema(crumbs, crumbs) }))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("BreadcrumbList: itemListElement.name | itemListElement.item.name");
    }

    [Fact]
    public void Nested_and_unknown_nodes_are_not_checked_for_properties()
    {
        SchemaRequirements.Missing(new SchemaNode { Type = "Organization", TopLevel = false, Properties = ["name"] }).ShouldBeEmpty();
        SchemaRequirements.Missing(Node("Event")).ShouldBeEmpty();
        SchemaRequirements.Missing(Node("OnlineStore", "name", "url")).ShouldBe(["logo"]);
    }

    [Fact]
    public void Missing_properties_are_listed_per_node()
    {
        var page = Page(GoodMarkup with { StructuredData = Schema(Node("Product", "name", "image", "offers", "offers.price"), Node("WebSite", "name")) });

        var violation = Run(new StructuredDataPropertiesRule(), page).ShouldHaveSingleItem();
        violation.CurrentValue.ShouldBe("Product: offers.priceCurrency | offers.priceSpecification.priceCurrency, WebSite: url");
        violation.Detail.ShouldBe("Product: offers.priceCurrency | offers.priceSpecification.priceCurrency, offers.availability · WebSite: url");
    }

    [Fact]
    public void Home_page_needs_organization_and_website()
    {
        Run(new SiteIdentitySchemaRule(), Page(GoodMarkup, path: "/")).ShouldHaveSingleItem().CurrentValue.ShouldBe("Eksik: Organization, WebSite");
        Run(new SiteIdentitySchemaRule(), Page(GoodMarkup with { StructuredData = Schema(Node("OnlineStore"), Node("WebSite")) }, path: "/")).ShouldBeEmpty();
        Run(new SiteIdentitySchemaRule(), Page(GoodMarkup, path: "/kategori")).ShouldBeEmpty();
        Run(new SiteIdentitySchemaRule(), Page(GoodMarkup, path: "/?utm_source=x")).ShouldBeEmpty();
    }

    [Fact]
    public void Every_page_but_the_home_page_needs_a_breadcrumb()
    {
        var noBreadcrumb = GoodMarkup with { StructuredData = Schema(Node("WebPage", "url")) };

        Run(new BreadcrumbSchemaRule(), Page(noBreadcrumb)).ShouldHaveSingleItem();
        Run(new BreadcrumbSchemaRule(), Page(noBreadcrumb, path: "/")).ShouldBeEmpty();
        Run(new BreadcrumbSchemaRule(), Page(noBreadcrumb with { StructuredData = noBreadcrumb.StructuredData with { Nodes = [new SchemaNode { Type = "BreadcrumbList" }] } })).ShouldBeEmpty();
    }

    [Fact]
    public void A_language_home_page_pointing_to_the_root_counts_as_home()
    {
        var languageHome = GoodMarkup with
        {
            StructuredData = Schema(Node("WebPage", "url")),
            Hreflangs = [new HreflangLink("tr", "https://example.com/"), new HreflangLink("en", "https://example.com/en")]
        };

        Run(new BreadcrumbSchemaRule(), Page(languageHome, path: "/en")).ShouldBeEmpty();
        Run(new BreadcrumbSchemaRule(), Page(languageHome, path: "/en/about")).ShouldHaveSingleItem();
        Run(new BreadcrumbSchemaRule(), Page(languageHome with { Hreflangs = [] }, path: "/en")).ShouldHaveSingleItem();
    }

    [Fact]
    public void Share_tags_must_all_be_present_and_absolute()
    {
        Run(new OpenGraphIncompleteRule(), Page(GoodMarkup with { OpenGraph = null })).ShouldHaveSingleItem().CurrentValue.ShouldBe("5 sorun");
        var relative = GoodMarkup with { OpenGraph = GoodMarkup.OpenGraph! with { Image = "/og.jpg", Type = null } };
        Run(new OpenGraphIncompleteRule(), Page(relative)).ShouldHaveSingleItem().Detail.ShouldBe("og:image mutlak adres değil, og:type yok");
    }

    [Fact]
    public void Share_image_needs_size_above_threshold_and_a_description()
    {
        var og = GoodMarkup.OpenGraph!;
        Run(new OpenGraphImageMetaRule(), Page(GoodMarkup with { OpenGraph = og with { ImageWidth = null } })).ShouldHaveSingleItem().CurrentValue.ShouldBe("boyut bildirilmemiş");
        Run(new OpenGraphImageMetaRule(), Page(GoodMarkup with { OpenGraph = og with { ImageWidth = "600", ImageHeight = "315", ImageAlt = "" } }))
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("600×315 px, og:image:alt yok");
        var lowered = Context(thresholds: new Dictionary<string, decimal> { ["minWidth"] = 600, ["minHeight"] = 315 });
        Run(new OpenGraphImageMetaRule(), lowered, Page(GoodMarkup with { OpenGraph = og with { ImageWidth = "600", ImageHeight = "315" } })).ShouldBeEmpty();
        Run(new OpenGraphImageMetaRule(), Page(GoodMarkup with { OpenGraph = og with { Image = null } })).ShouldBeEmpty();
    }

    [Fact]
    public void Share_address_must_match_canonical_ignoring_case_fragment_and_trailing_slash()
    {
        var og = GoodMarkup.OpenGraph!;
        Run(new OpenGraphUrlMismatchRule(), Page(GoodMarkup with { OpenGraph = og with { Url = "https://example.com/eski-sayfa" } })).ShouldHaveSingleItem();
        Run(new OpenGraphUrlMismatchRule(), Page(GoodMarkup with { OpenGraph = og with { Url = "https://Example.com/sayfa/#x" } })).ShouldBeEmpty();
    }

    [Fact]
    public void Meta_description_length_uses_thresholds()
    {
        Run(new MetaDescriptionLengthRule(), Page(GoodMarkup with { MetaDescription = "Kısa açıklama." })).ShouldHaveSingleItem().CurrentValue.ShouldBe("14 karakter");
        Run(new MetaDescriptionLengthRule(), Page(GoodMarkup with { MetaDescription = new string('a', 161) })).ShouldHaveSingleItem();
        Run(new MetaDescriptionLengthRule(), Page(GoodMarkup with { MetaDescription = null })).ShouldBeEmpty();
    }

    [Fact]
    public void Element_issue_rules_report_count_and_samples()
    {
        var cases = new (IPageRule Rule, PageSnapshotData Data)[]
        {
            (new LinkNoTextRule(), GoodMarkup with { Links = new LinkFacts { NoText = Issue("a[href=\"/sepet\"]", "a[href=\"/favori\"]") } }),
            (new EmptyHrefRule(), GoodMarkup with { Links = new LinkFacts { EmptyHref = Issue("a[href=\"#\"]", "a[href=\"#\"] \"Filtrele\"") } }),
            (new BlankNoopenerRule(), GoodMarkup with { Links = new LinkFacts { BlankWithoutNoopener = Issue("a", "b") } }),
            (new TableHeaderRule(), GoodMarkup with { Tables = new TableFacts { Tables = 2, WithoutHeader = Issue("table", "table") } }),
            (new IframeTitleRule(), GoodMarkup with { Iframes = new IframeFacts { Iframes = 2, WithoutTitle = Issue("iframe", "iframe") } }),
            (new IframeLazyRule(), GoodMarkup with { Iframes = new IframeFacts { Iframes = 2, NotLazy = Issue("iframe", "iframe") } }),
            (new FormLabelRule(), GoodMarkup with { Forms = new FormFacts { Fields = 2, Unlabeled = Issue("input", "select") } }),
            (new NavLabelRule(), GoodMarkup with { Landmarks = GoodMarkup.Landmarks with { Nav = 2, UnlabeledNavs = Issue("nav", "nav") } })
        };

        foreach (var (rule, data) in cases)
        {
            var violation = Run(rule, Page(data)).ShouldHaveSingleItem(rule.Code);
            violation.CurrentValue.ShouldBe("2 öğe", rule.Code);
            violation.Detail!.ShouldContain(" · ", Case.Sensitive, rule.Code);
        }
    }

    [Fact]
    public void Generic_link_texts_use_turkish_list_only_on_turkish_pages()
    {
        var links = new LinkFacts { ShortTexts = [new LinkTextCount("devamını oku", 3), new LinkTextCount("read more", 1), new LinkTextCount("kış lastikleri", 4)] };

        var violation = Run(new GenericLinkTextRule(), Page(GoodMarkup with { Links = links })).ShouldHaveSingleItem();
        violation.CurrentValue.ShouldBe("4 link");
        violation.Detail.ShouldBe("\"devamını oku\" ×3 · \"read more\" ×1");
        Run(new GenericLinkTextRule(), Page(GoodMarkup with { Links = links, Lang = "en" })).ShouldHaveSingleItem().CurrentValue.ShouldBe("1 link");
    }

    [Fact]
    public void Heading_levels_must_not_skip_and_must_not_be_empty()
    {
        var violation = Run(new HeadingSkipRule(), Page(GoodMarkup with { Headings = new HeadingFacts { Levels = [1, 2, 4, 2, 4], Empty = Issue("h3") } }))
            .ShouldHaveSingleItem();
        violation.CurrentValue.ShouldBe("H2 → H4 · 1 boş başlık");
        violation.Detail.ShouldBe("Sıra: H1 H2 H4 H2 H4");
        Run(new HeadingSkipRule(), Page(GoodMarkup with { Headings = new HeadingFacts { Levels = [2, 3, 1, 2] } })).ShouldBeEmpty();
    }

    [Fact]
    public void Page_needs_exactly_one_main_and_some_other_landmark()
    {
        Run(new LandmarksRule(), Page(GoodMarkup with { Landmarks = new LandmarkFacts() })).ShouldHaveSingleItem().CurrentValue.ShouldBe("main yok, header, nav ve footer yok");
        Run(new LandmarksRule(), Page(GoodMarkup with { Landmarks = new LandmarkFacts { Main = 2, Footer = 1 } })).ShouldHaveSingleItem().CurrentValue.ShouldBe("2 main");
    }

    [Theory]
    [InlineData(null, "Viewport yok")]
    [InlineData("width=1024", "width=device-width yok")]
    [InlineData("width=device-width, initial-scale=1, user-scalable=no", "yakınlaştırma kapalı")]
    [InlineData("width=device-width, maximum-scale=1", "maximum-scale=1")]
    public void Viewport_must_fit_the_device_and_allow_zoom(string? viewport, string expected)
    {
        Run(new ViewportRule(), Page(GoodMarkup with { Viewport = viewport })).ShouldHaveSingleItem().CurrentValue.ShouldBe(expected);
    }

    [Fact]
    public void Viewport_allowing_zoom_up_to_five_passes()
    {
        Run(new ViewportRule(), Page(GoodMarkup with { Viewport = "width=device-width; initial-scale=1; maximum-scale=5" })).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, "lang yok")]
    [InlineData("türkçe", "Geçersiz: \"türkçe\"")]
    public void Page_language_must_be_a_valid_tag(string? lang, string expected)
    {
        Run(new LangRule(), Page(GoodMarkup with { Lang = lang })).ShouldHaveSingleItem().CurrentValue.ShouldBe(expected);
        Run(new LangRule(), Page(GoodMarkup with { Lang = "tr-TR" })).ShouldBeEmpty();
    }

    [Fact]
    public void New_rules_start_strict()
    {
        RuleCatalog.Find("seo.schema.missing")!.DefaultSeverity.ShouldBe(Severity.High);
        RuleCatalog.Find("a11y.form.no_label")!.DefaultSeverity.ShouldBe(Severity.High);
        RuleCatalog.Find("content.image.alt_missing")!.DefaultSeverity.ShouldBe(Severity.Medium);
        RuleCatalog.All.Where(r => r.Category == FindingCategory.Accessibility).Count().ShouldBe(14);
    }

    [Fact]
    public void Mixed_content_lists_http_resources_on_https_pages()
    {
        var mixed = Good with
        {
            Images = [new ImageInfo("http://cdn.example.com/a.jpg", "a", "1", "1", null), new ImageInfo("/b.jpg", "b", "1", "1", null), new ImageInfo("//cdn.example.com/c.jpg", "c", "1", "1", null)],
            Scripts = [new ScriptInfo("http://eski.example.com/x.js", false, false, true), new ScriptInfo(null, false, false, true)],
            Stylesheets = ["https://example.com/site.css", "HTTP://eski.example.com/x.css"]
        };

        var violation = Run(new MixedContentRule(), Page(mixed)).ShouldHaveSingleItem();

        violation.CurrentValue.ShouldBe("3 kaynak");
        violation.Detail.ShouldBe("http://cdn.example.com/a.jpg · http://eski.example.com/x.js · HTTP://eski.example.com/x.css");
        Run(new MixedContentRule(), Page(Good with { Images = [new ImageInfo("https://cdn.example.com/a.jpg", "a", "1", "1", null)] })).ShouldBeEmpty();
    }
}
