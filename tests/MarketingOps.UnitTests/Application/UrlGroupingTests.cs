using MarketingOps.Application.PageTypes.Suggestions;

namespace MarketingOps.UnitTests.Application;

public class UrlGroupingTests
{
    [Fact]
    public void Root_slugs_group_by_extension_and_nested_pages_by_first_folder()
    {
        var groups = UrlGrouping.Group(
        [
            "https://ornek.com/",
            "https://ornek.com/michelin-pilot.html",
            "https://ornek.com/bridgestone-turanza.HTML",
            "https://ornek.com/goodyear-eagle.html",
            "https://ornek.com/kategori/yaz",
            "https://ornek.com/kategori/kis/suv",
            "https://ornek.com/iletisim",
            "https://ornek.com/kategori/yaz?sayfa=2"
        ]);

        groups.Select(g => (g.Pattern, g.Count)).ShouldBe(
        [
            ("/*.html", 3),
            ("/kategori/**", 2),
            ("/", 1),
            ("/*", 1)
        ]);
    }

    [Fact]
    public void Root_slugs_with_a_type_marker_group_by_the_marker()
    {
        var groups = UrlGrouping.Group(
        [
            "https://ornek.com/285-60r18-116v-hankook-rh06-ventus-st-p-hnkk-1003726",
            "https://ornek.com/255-60r18-112h-xl-pirelli-scorpion-verde-a-s-p-prll-1862400",
            "https://ornek.com/lastik-c-283608",
            "https://ornek.com/235-65-r17-kis-lastigi"
        ]);

        groups.Select(g => (g.Pattern, g.Count)).ShouldBe([("/*-p-*", 2), ("/*", 1), ("/*-c-*", 1)]);
    }

    [Fact]
    public void Groups_keep_a_few_samples_and_ignore_invalid_urls()
    {
        var urls = Enumerable.Range(1, 30).Select(i => $"https://ornek.com/urun/p{i}").Append("bozuk adres");

        var group = UrlGrouping.Group(urls).ShouldHaveSingleItem();

        group.Count.ShouldBe(30);
        group.SampleUrls.Count.ShouldBe(5);
    }
}
