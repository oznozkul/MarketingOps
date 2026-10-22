using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Sites;

public class PageTypeMatchTests
{
    private static readonly string[] NoSchema = [];

    private static PageType Build(MatchMode mode, params MatchCondition[] conditions)
    {
        var site = Site.Create(Guid.CreateVersion7(), "Test", "lastikborsasi.com", DateTimeOffset.UtcNow);
        return site.AddPageType("Tip", mode, conditions);
    }

    private static Uri Url(string path) => new("https://lastikborsasi.com" + path);

    [Theory]
    [InlineData("/urun/michelin-primacy-4", true)]
    [InlineData("/urun/michelin-primacy-4/", true)]
    [InlineData("/URUN/Michelin", true)]
    [InlineData("/urun/", false)]
    [InlineData("/urun/a/b", false)]
    [InlineData("/kategori/urun", false)]
    public void Single_star_matches_exactly_one_segment(string path, bool expected) =>
        Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlGlob, "/urun/*")).Matches(Url(path), NoSchema).ShouldBe(expected);

    [Theory]
    [InlineData("/arac/volkswagen", true)]
    [InlineData("/arac/volkswagen/passat/2019", true)]
    [InlineData("/arac/", true)]
    [InlineData("/araclar/volkswagen", false)]
    public void Double_star_matches_many_segments(string path, bool expected) =>
        Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlGlob, "/arac/**")).Matches(Url(path), NoSchema).ShouldBe(expected);

    [Theory]
    [InlineData("/lastik/205-55-r16/michelin", true)]
    [InlineData("/lastik/205-55-r16", false)]
    public void Star_slash_star_requires_two_segments(string path, bool expected) =>
        Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlGlob, "/lastik/*/*")).Matches(Url(path), NoSchema).ShouldBe(expected);

    [Fact]
    public void Glob_ignores_query_string() =>
        Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlGlob, "/jant/*")).Matches(Url("/jant/17-inc?sirala=fiyat"), NoSchema).ShouldBeTrue();

    [Theory]
    [InlineData("/lastik/205-55-r16", true)]
    [InlineData("/lastik/205-55-r16?marka=michelin", true)]
    [InlineData("/lastik/yaz", false)]
    public void Regex_matches_path_and_query(string path, bool expected) =>
        Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlRegex, @"^/lastik/\d{3}-\d{2}-r\d{2}")).Matches(Url(path), NoSchema).ShouldBe(expected);

    [Fact]
    public void Schema_type_matches_case_insensitively()
    {
        var pageType = Build(MatchMode.Any, new MatchCondition(MatchConditionType.SchemaType, "Product"));

        pageType.Matches(Url("/x"), ["BreadcrumbList", "product"]).ShouldBeTrue();
        pageType.Matches(Url("/x"), ["Article"]).ShouldBeFalse();
    }

    [Fact]
    public void All_mode_requires_every_condition()
    {
        var pageType = Build(MatchMode.All, new MatchCondition(MatchConditionType.UrlGlob, "/urun/*"), new MatchCondition(MatchConditionType.SchemaType, "Product"));

        pageType.Matches(Url("/urun/abc"), ["Product"]).ShouldBeTrue();
        pageType.Matches(Url("/urun/abc"), NoSchema).ShouldBeFalse();
        pageType.Matches(Url("/blog/abc"), ["Product"]).ShouldBeFalse();
    }

    [Fact]
    public void Any_mode_needs_one_condition()
    {
        var pageType = Build(MatchMode.Any, new MatchCondition(MatchConditionType.UrlGlob, "/urun/*"), new MatchCondition(MatchConditionType.SchemaType, "Product"));

        pageType.Matches(Url("/urun/abc"), NoSchema).ShouldBeTrue();
        pageType.Matches(Url("/baska"), ["Product"]).ShouldBeTrue();
        pageType.Matches(Url("/baska"), NoSchema).ShouldBeFalse();
    }
}
