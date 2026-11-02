using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class HtmlSnapshotParserTests
{
    private static readonly Uri PageUrl = new("https://lastikborsasi.com/urun/michelin-primacy-4-205-55-r16");

    private static ParsedPage ParseFixture() => HtmlSnapshotParser.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "product-page.html")),
        PageUrl,
        u => u.Host is "lastikborsasi.com" or "www.lastikborsasi.com");

    [Fact]
    public void Extracts_head_metadata()
    {
        var data = ParseFixture().Data;

        data.Title.ShouldBe("Michelin Primacy 4 205/55 R16 91V Lastik Fiyatı | Lastik Borsası");
        data.MetaDescription.ShouldBe("Michelin Primacy 4 yaz lastiği, hızlı kargo.");
        data.Canonical.ShouldBe("https://lastikborsasi.com/urun/michelin-primacy-4-205-55-r16");
        data.MetaRobots.ShouldBe("index, follow");
        data.Lang.ShouldBe("tr");
        data.Charset.ShouldBe("utf-8");
        data.Viewport.ShouldNotBeNull();
        data.OpenGraph!.Title.ShouldBe("Michelin Primacy 4");
    }

    [Fact]
    public void Extracts_headings_and_schema_types_from_graph()
    {
        var data = ParseFixture().Data;

        data.H1.ShouldBe(["Michelin Primacy 4 205/55 R16"]);
        data.H2.Count.ShouldBe(2);
        data.SchemaTypes.ShouldBe(["Product", "BreadcrumbList"]);
    }

    [Fact]
    public void Separates_internal_and_external_links()
    {
        var parsed = ParseFixture();

        parsed.InternalLinks.Select(UrlNormalizer.Normalize).Distinct().Count().ShouldBe(parsed.InternalLinks.Count);
        parsed.Data.InternalLinks.ShouldContain("https://www.lastikborsasi.com/jant/17-inc");
        parsed.Data.InternalLinks.ShouldContain("https://lastikborsasi.com/kategori/yaz-lastikleri");
        parsed.Data.ExternalLinks.ShouldBe(["https://www.michelin.com.tr/"]);
        parsed.Data.InternalLinks.ShouldNotContain(l => l.StartsWith("mailto") || l.StartsWith("javascript"));
    }

    [Fact]
    public void Captures_images_scripts_and_resource_hints()
    {
        var data = ParseFixture().Data;

        data.Images.Count.ShouldBe(2);
        data.Images[1].Width.ShouldBeNull();
        data.Images[1].Alt.ShouldBeNull();

        var jquery = data.Scripts.Single(s => s.Src?.EndsWith("jquery.min.js") == true);
        jquery.InHead.ShouldBeTrue();
        jquery.Async.ShouldBeFalse();
        jquery.Defer.ShouldBeFalse();
        data.Scripts.Single(s => s.Src?.EndsWith("app.js") == true).Defer.ShouldBeTrue();
        data.Scripts.Single(s => s.Src?.Contains("gtm.js") == true).InHead.ShouldBeFalse();

        data.Stylesheets.ShouldBe(["https://lastikborsasi.com/css/site.css"]);
        data.Preloads.ShouldHaveSingleItem().As.ShouldBe("image");
        data.Preconnects.ShouldBe(["https://cdn.lastikborsasi.com"]);
    }

    [Fact]
    public void Scripts_the_browser_does_not_run_are_ignored()
    {
        var data = HtmlSnapshotParser.Parse("""
            <html><head>
            <script type="text/plain" data-category="analytics" src="https://www.googletagmanager.com/gtag/js?id=G-1"></script>
            <script type="text/x-template" id="kart"><a href="#">x</a></script>
            <script type="module" src="/app.mjs"></script>
            <script type="text/javascript" src="/eski.js"></script>
            </head><body></body></html>
            """, new Uri("https://ornek.com/"), _ => true).Data;

        data.Scripts.Select(s => s.Src).ShouldBe(["https://ornek.com/app.mjs", "https://ornek.com/eski.js"]);
    }

    [Fact]
    public void Counts_words_without_script_content()
    {
        var count = ParseFixture().Data.WordCount;

        count.ShouldBeGreaterThan(15);
        count.ShouldBeLessThan(40);
    }
}
