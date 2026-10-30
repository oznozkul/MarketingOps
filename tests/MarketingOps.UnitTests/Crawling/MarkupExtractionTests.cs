using MarketingOps.Domain.Crawls;
using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class MarkupExtractionTests
{
    private static readonly Uri PageUrl = new("https://lastikborsasi.com/kis");

    private static PageSnapshotData ParseFixture() => HtmlSnapshotParser.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "markup-page.html")),
        PageUrl,
        u => u.Host == "lastikborsasi.com").Data;

    private static PageSnapshotData Parse(string body, string head = "") => HtmlSnapshotParser.Parse(
        $"<!doctype html><html><head>{head}</head><body>{body}</body></html>", PageUrl, _ => true).Data;

    [Fact]
    public void Marks_the_snapshot_with_the_markup_version()
    {
        ParseFixture().Version.ShouldBe(PageSnapshotData.MarkupVersion);
    }

    [Fact]
    public void Reads_share_image_size_and_description()
    {
        var og = ParseFixture().OpenGraph!;

        og.ImageWidth.ShouldBe("1200");
        og.ImageHeight.ShouldBe("630");
        og.ImageAlt.ShouldBe("Kış lastiği");
    }

    [Fact]
    public void Splits_json_ld_into_nodes_and_counts_broken_blocks()
    {
        var sd = ParseFixture().StructuredData;

        sd.JsonLdBlocks.ShouldBe(2);
        sd.Invalid.Count.ShouldBe(1);
        sd.Invalid.Samples.Single().ShouldStartWith("{ \"@type\": \"Product\"");
        sd.IsPresent().ShouldBeTrue();

        var product = sd.Nodes.Single(n => n.Type == "Product");
        product.TopLevel.ShouldBeTrue();
        product.Properties.ShouldContain("offers.price");
        product.Properties.ShouldContain("offers.priceCurrency");
        product.Properties.ShouldNotContain("offers.availability");
        sd.Nodes.ShouldContain(n => n.Type == "Thing");

        // Boş logo dolu sayılmaz.
        sd.Nodes.Single(n => n.Type == "Organization").Properties.ShouldNotContain("logo");

        // İç içe düğüm varlık için sayılır, üst düzey değildir.
        var breadcrumb = sd.Nodes.Single(n => n.Type == "BreadcrumbList");
        breadcrumb.TopLevel.ShouldBeFalse();
        breadcrumb.Properties.ShouldContain("itemListElement.position");
        sd.Nodes.Single(n => n.Type == "Offer").TopLevel.ShouldBeFalse();
    }

    [Fact]
    public void Microdata_counts_as_structured_data()
    {
        var sd = Parse("""<div itemscope itemtype="https://schema.org/Product"><span itemprop="name">A</span></div>""").StructuredData;

        sd.MicrodataTypes.ShouldBe(["Product"]);
        sd.IsPresent().ShouldBeTrue();
        Parse("<p>Yok</p>").StructuredData.IsPresent().ShouldBeFalse();
    }

    [Fact]
    public void Finds_links_without_an_accessible_name()
    {
        var links = ParseFixture().Links;

        // Boş alt'lı görsel linki tek başına ad vermez; gizli öğelerdeki linkler sayılmaz.
        links.NoText.Count.ShouldBe(1);
        links.NoText.Samples.Single().ShouldContain("/kis/bos");
    }

    [Fact]
    public void Finds_links_without_a_destination_except_buttons_and_current_page()
    {
        var links = ParseFixture().Links;

        links.EmptyHref.Count.ShouldBe(2);
        links.EmptyHref.Samples.ShouldContain(s => s.Contains("Filtrele"));
        links.EmptyHref.Samples.ShouldContain(s => s.Contains("Karşılaştır"));
    }

    [Fact]
    public void Finds_new_tab_links_without_noopener()
    {
        var links = ParseFixture().Links;

        links.BlankWithoutNoopener.Count.ShouldBe(1);
        links.BlankWithoutNoopener.Samples.Single().ShouldContain("michelin.com.tr");
    }

    [Fact]
    public void Counts_short_link_texts_case_and_space_insensitively()
    {
        ParseFixture().Links.ShortTexts.ShouldContain(new LinkTextCount("devamını oku", 2));
    }

    [Fact]
    public void Records_heading_levels_in_order_and_empty_headings()
    {
        var headings = ParseFixture().Headings;

        headings.Levels.ShouldBe([1, 2, 4, 3]);
        headings.Empty.Count.ShouldBe(1);
        Parse("""<div role="heading" aria-level="2">Başlık</div>""").Headings.Levels.ShouldBe([2]);
    }

    [Fact]
    public void Finds_form_fields_without_a_label_ignoring_placeholder_and_non_fields()
    {
        var forms = ParseFixture().Forms;

        forms.Fields.ShouldBe(4);
        forms.Unlabeled.Count.ShouldBe(1);
        forms.Unlabeled.Samples.Single().ShouldContain("email");
    }

    [Fact]
    public void Finds_data_tables_without_header_cells()
    {
        var tables = ParseFixture().Tables;

        tables.Tables.ShouldBe(2);
        tables.WithoutHeader.Count.ShouldBe(1);
    }

    [Fact]
    public void Finds_iframes_without_title_or_lazy_loading()
    {
        var iframes = ParseFixture().Iframes;

        iframes.Iframes.ShouldBe(2);
        iframes.WithoutTitle.Count.ShouldBe(1);
        iframes.NotLazy.Count.ShouldBe(1);
        iframes.NotLazy.Samples.Single().ShouldContain("maps.google.com");
    }

    [Fact]
    public void Counts_landmarks_and_unlabeled_navs()
    {
        var landmarks = ParseFixture().Landmarks;

        landmarks.Main.ShouldBe(1);
        landmarks.Header.ShouldBe(1);
        landmarks.Footer.ShouldBe(1);
        landmarks.Nav.ShouldBe(3);
        landmarks.UnlabeledNavs.Count.ShouldBe(2);
        Parse("<nav><a href='/'>A</a></nav>").Landmarks.UnlabeledNavs.Count.ShouldBe(0);
    }
}
