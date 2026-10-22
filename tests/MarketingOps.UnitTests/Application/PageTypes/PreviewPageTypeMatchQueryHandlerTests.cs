using MarketingOps.Application.PageTypes;
using MarketingOps.Application.PageTypes.Queries.PreviewPageTypeMatch;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Application.PageTypes;

public class PreviewPageTypeMatchQueryHandlerTests
{
    // SiteId verilmediğinde veritabanına gidilmez; çakışma senaryosu entegrasyon testinde.
    private readonly PreviewPageTypeMatchQueryHandler _handler = new(null!);

    [Fact]
    public async Task Reports_which_samples_match()
    {
        var query = new PreviewPageTypeMatchQuery(
            MatchMode.All,
            [new(MatchConditionType.UrlGlob, "/urun/*"), new(MatchConditionType.SchemaType, "Product")],
            [
                new("https://lastikborsasi.com/urun/a", ["Product"]),
                new("https://lastikborsasi.com/urun/b"),
                new("https://lastikborsasi.com/blog/c", ["Product"])
            ]);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.TotalCount.ShouldBe(3);
        result.MatchedCount.ShouldBe(1);
        result.Results.Select(r => r.IsMatch).ShouldBe([true, false, false]);
    }

    [Fact]
    public async Task Invalid_regex_surfaces_as_domain_error() =>
        await Should.ThrowAsync<DomainException>(() => _handler.Handle(
            new PreviewPageTypeMatchQuery(MatchMode.Any, [new MatchConditionInput(MatchConditionType.UrlRegex, "([")], [new("https://a.com/x")]),
            CancellationToken.None));
}
