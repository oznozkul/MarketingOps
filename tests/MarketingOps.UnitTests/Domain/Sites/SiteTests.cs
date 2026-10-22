using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Sites;

public class SiteTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 9, 0, 0, TimeSpan.Zero);
    private static readonly MatchCondition[] AnyCondition = [new(MatchConditionType.UrlGlob, "/urun/*")];

    private static Site NewSite() => Site.Create(Guid.CreateVersion7(), "Lastik Borsası", "lastikborsasi.com", Now);

    [Theory]
    [InlineData("lastikborsasi.com", "lastikborsasi.com")]
    [InlineData("LastikBorsasi.com", "lastikborsasi.com")]
    [InlineData("https://www.lastikborsasi.com/", "lastikborsasi.com")]
    [InlineData("http://lastikborsasi.com/kategori/yaz?x=1", "lastikborsasi.com")]
    [InlineData("  www.shop.tyresupply.co.uk  ", "shop.tyresupply.co.uk")]
    public void Domain_is_normalized(string input, string expected) =>
        Site.NormalizeDomain(input).ShouldBe(expected);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("localhost")]
    [InlineData("not a domain.com")]
    [InlineData("https://")]
    public void Invalid_domain_is_rejected(string input) =>
        Should.Throw<DomainException>(() => Site.NormalizeDomain(input));

    [Fact]
    public void Create_sets_defaults_and_trims_name()
    {
        var site = Site.Create(Guid.CreateVersion7(), "  Tyre Supply ", "https://tyresupply.com", Now);

        site.Name.ShouldBe("Tyre Supply");
        site.Domain.ShouldBe("tyresupply.com");
        site.CreatedAt.ShouldBe(Now);
        site.IsOwnershipVerified.ShouldBeFalse();
        site.CrawlSettings.RespectRobotsTxt.ShouldBeTrue();
        site.PageTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Empty_name_is_rejected() =>
        Should.Throw<DomainException>(() => Site.Create(Guid.CreateVersion7(), " ", "lastikborsasi.com", Now));

    [Fact]
    public void Page_type_names_are_unique_case_insensitively()
    {
        var site = NewSite();
        site.AddPageType("Ürün", MatchMode.Any, AnyCondition);

        var ex = Should.Throw<DomainException>(() => site.AddPageType("  ürün ", MatchMode.Any, AnyCondition));
        ex.Message.ShouldContain("zaten var");
    }

    [Fact]
    public void Page_type_requires_at_least_one_condition() =>
        Should.Throw<DomainException>(() => NewSite().AddPageType("Ürün", MatchMode.Any, []));

    [Fact]
    public void Updating_a_page_type_to_another_types_name_is_rejected()
    {
        var site = NewSite();
        site.AddPageType("Ürün", MatchMode.Any, AnyCondition);
        var blog = site.AddPageType("Blog", MatchMode.Any, [new(MatchConditionType.UrlGlob, "/blog/*")]);

        Should.Throw<DomainException>(() => site.UpdatePageType(blog.Id, "ÜRÜN", MatchMode.Any, AnyCondition));
    }

    [Fact]
    public void Updating_a_page_type_keeping_its_own_name_is_allowed()
    {
        var site = NewSite();
        var product = site.AddPageType("Ürün", MatchMode.Any, AnyCondition);

        site.UpdatePageType(product.Id, "Ürün", MatchMode.All, [new(MatchConditionType.SchemaType, "Product")]);

        product.Mode.ShouldBe(MatchMode.All);
        product.Conditions.ShouldHaveSingleItem().Type.ShouldBe(MatchConditionType.SchemaType);
    }

    [Fact]
    public void Removing_an_unknown_page_type_is_rejected() =>
        Should.Throw<DomainException>(() => NewSite().RemovePageType(Guid.CreateVersion7()));

    [Fact]
    public void Remove_page_type_removes_it()
    {
        var site = NewSite();
        var product = site.AddPageType("Ürün", MatchMode.Any, AnyCondition);

        site.RemovePageType(product.Id);

        site.PageTypes.ShouldBeEmpty();
    }

    [Fact]
    public void Duplicate_conditions_are_collapsed()
    {
        var site = NewSite();
        var pageType = site.AddPageType("Ürün", MatchMode.Any, [new(MatchConditionType.UrlGlob, "/urun/*"), new(MatchConditionType.UrlGlob, "/urun/*")]);

        pageType.Conditions.Count.ShouldBe(1);
    }

    [Fact]
    public void Find_matching_page_types_reports_overlaps_except_the_given_one()
    {
        var site = NewSite();
        var product = site.AddPageType("Ürün", MatchMode.Any, AnyCondition);
        site.AddPageType("Tüm ürünler", MatchMode.Any, [new(MatchConditionType.UrlGlob, "/urun/**")]);
        site.AddPageType("Blog", MatchMode.Any, [new(MatchConditionType.UrlGlob, "/blog/*")]);

        var url = new Uri("https://lastikborsasi.com/urun/michelin");

        site.FindMatchingPageTypes(url, []).Select(p => p.Name).ShouldBe(["Ürün", "Tüm ürünler"]);
        site.FindMatchingPageTypes(url, [], product.Id).Select(p => p.Name).ShouldBe(["Tüm ürünler"]);
    }
}
