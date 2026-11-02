using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class SitemapParserTests
{
    [Fact]
    public void Reads_url_set()
    {
        var content = SitemapParser.Parse("""
            <?xml version="1.0" encoding="UTF-8"?>
            <urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <url><loc>https://a.com/urun/1</loc><lastmod>2026-10-01</lastmod></url>
              <url><loc> https://a.com/urun/2 </loc></url>
            </urlset>
            """);

        content.Urls.ShouldBe(["https://a.com/urun/1", "https://a.com/urun/2"]);
        content.Sitemaps.ShouldBeEmpty();
    }

    [Fact]
    public void Reads_sitemap_index()
    {
        var content = SitemapParser.Parse("""
            <sitemapindex xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
              <sitemap><loc>https://a.com/sitemap-urun.xml</loc></sitemap>
            </sitemapindex>
            """);

        content.Sitemaps.ShouldBe(["https://a.com/sitemap-urun.xml"]);
        content.Urls.ShouldBeEmpty();
    }

    [Fact]
    public void Byte_order_mark_and_leading_whitespace_before_the_declaration_are_ignored()
    {
        // Gövde UTF-8 BOM'uyla geliyor (ör. IIS); GetString onu metnin başında bırakır, XML ayrıştırıcı da reddeder.
        var content = SitemapParser.Parse("\uFEFF\r\n<?xml version=\"1.0\" encoding=\"utf-8\"?><sitemapindex xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"><sitemap><loc>https://a.com/sitemaps/lastik.xml</loc></sitemap></sitemapindex>");

        content.Sitemaps.ShouldBe(["https://a.com/sitemaps/lastik.xml"]);
    }

    [Fact]
    public void Broken_xml_yields_nothing() =>
        SitemapParser.Parse("<html>bu bir sitemap değil").Urls.ShouldBeEmpty();
}
