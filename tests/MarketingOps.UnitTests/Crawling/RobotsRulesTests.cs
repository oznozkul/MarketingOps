using MarketingOps.Infrastructure.Crawling;

namespace MarketingOps.UnitTests.Crawling;

public class RobotsRulesTests
{
    private const string Agent = "MarketingOpsBot/1.0 (+https://marketingops.app/bot)";

    private const string Robots = """
        # yorum
        User-agent: *
        Disallow: /hesabim/
        Disallow: /*?siralama=
        Disallow: /*.pdf$
        Allow: /hesabim/giris
        Sitemap: https://lastikborsasi.com/sitemap.xml

        User-agent: Googlebot
        Disallow: /
        """;

    private static bool Allowed(string content, string path) =>
        RobotsRules.Parse(content, Agent).IsAllowed(new Uri("https://lastikborsasi.com" + path));

    [Theory]
    [InlineData("/urun/a", true)]
    [InlineData("/hesabim/siparisler", false)]
    [InlineData("/hesabim/giris", true)]
    [InlineData("/kategori/yaz?siralama=fiyat", false)]
    [InlineData("/kategori/yaz?sayfa=2", true)]
    [InlineData("/katalog.pdf", false)]
    [InlineData("/katalog.pdf?v=2", true)]
    public void Applies_star_group_with_longest_match(string path, bool expected) =>
        Allowed(Robots, path).ShouldBe(expected);

    [Fact]
    public void Our_own_group_takes_precedence_over_star()
    {
        const string content = """
            User-agent: *
            Disallow: /

            User-agent: MarketingOpsBot
            Disallow: /gizli/
            """;

        Allowed(content, "/urun/a").ShouldBeTrue();
        Allowed(content, "/gizli/x").ShouldBeFalse();
    }

    [Fact]
    public void Empty_disallow_allows_everything() =>
        Allowed("User-agent: *\nDisallow:", "/her-sey").ShouldBeTrue();

    [Fact]
    public void Sitemaps_are_collected() =>
        RobotsRules.Parse(Robots, Agent).Sitemaps.ShouldBe(["https://lastikborsasi.com/sitemap.xml"]);
}
