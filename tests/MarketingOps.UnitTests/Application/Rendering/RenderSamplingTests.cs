using MarketingOps.Application.Crawls.Commands.RenderCrawl;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Application.Rendering;

public class RenderSamplingTests
{
    private static readonly Guid Product = Guid.CreateVersion7();
    private static readonly Guid Category = Guid.CreateVersion7();

    private static RenderCandidate Page(string path, Guid? type) => new($"https://example.com{path}", type);

    [Fact]
    public void Picks_spread_out_samples_per_page_type_and_always_the_home_page()
    {
        var products = Enumerable.Range(1, 10).Select(i => Page($"/urun/{new string('a', i)}", Product)).ToList();
        var pages = products.Append(Page("/kategori/kis", Category)).Append(Page("/", null)).Append(Page("/eslesmeyen", null)).ToList();

        var selected = RenderSampling.Select(pages, 3);

        selected[0].Url.ShouldBe("https://example.com/");
        selected.Where(p => p.PageTypeId == Product).Select(p => p.Url)
            .ShouldBe([products[0].Url, products[5].Url, products[9].Url]);    // en kısa, orta, en uzun adres
        selected.ShouldContain(p => p.PageTypeId == Category);
        selected.ShouldNotContain(p => p.Url.EndsWith("/eslesmeyen"));
    }

    [Fact]
    public void A_page_type_with_fewer_pages_than_samples_gives_all_of_them_and_home_is_not_repeated()
    {
        var pages = new[] { Page("/", Category), Page("/kategori/kis", Category) };

        RenderSampling.Select(pages, 3).Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Render_samples_outside_range_are_rejected(int samples) =>
        Should.Throw<DomainException>(() => new CrawlSettings(CrawlSchedule.Daily, 2, 1000, [], true, samples));

    [Fact]
    public void Render_is_on_with_three_samples_by_default()
    {
        var settings = CrawlSettings.Default;

        settings.RenderEnabled.ShouldBeTrue();
        settings.RenderSamplesPerPageType.ShouldBe(3);
    }
}
