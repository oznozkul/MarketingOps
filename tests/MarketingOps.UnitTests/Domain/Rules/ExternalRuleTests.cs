using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Rules;
using MarketingOps.Domain.Rules.Definitions;
using static MarketingOps.UnitTests.Domain.Rules.RuleFixture;

namespace MarketingOps.UnitTests.Domain.Rules;

/// <summary>Search Console, URL Inspection ve PageSpeed verisi kullanan kurallar.</summary>
public class ExternalRuleTests
{
    private static RuleContext With(ExternalData data, IReadOnlyDictionary<string, decimal>? thresholds = null) =>
        new(NewSite(), thresholds ?? new Dictionary<string, decimal>(), new HashSet<string>(), [], data);

    private static ExternalData Data(Dictionary<string, SearchMetrics>? search = null, Dictionary<string, IndexInfo>? index = null, List<VitalsSample>? vitals = null) =>
        new(new HashSet<RuleDataSource> { RuleDataSource.Crawl, RuleDataSource.SearchConsole, RuleDataSource.UrlInspection, RuleDataSource.PageSpeed },
            search ?? [], index ?? [], vitals ?? []);

    [Fact]
    public void Opportunity_flags_pages_just_off_the_first_page_with_enough_impressions()
    {
        var page = Page(path: "/a");
        Run(new OpportunityRule(), With(Data(new() { [page.NormalizedUrl] = new SearchMetrics(12, 840, 13.4) })), page)
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("13,4. sıra");
        Run(new OpportunityRule(), With(Data(new() { [page.NormalizedUrl] = new SearchMetrics(12, 40, 13.4) })), page).ShouldBeEmpty();   // az gösterim
        Run(new OpportunityRule(), With(Data(new() { [page.NormalizedUrl] = new SearchMetrics(90, 840, 4.2) })), page).ShouldBeEmpty();   // zaten ilk sayfada
        Run(new OpportunityRule(), With(Data()), page).ShouldBeEmpty();                                                                   // veri yok
    }

    [Fact]
    public void Not_indexed_flags_indexable_pages_google_did_not_index()
    {
        var page = Page(path: "/b");
        Run(new NotIndexedRule(), With(Data(index: new() { [page.NormalizedUrl] = new IndexInfo("NEUTRAL", "Tarandı - şu anda dizine eklenmemiş") })), page)
            .ShouldHaveSingleItem().CurrentValue.ShouldBe("Tarandı - şu anda dizine eklenmemiş");
        Run(new NotIndexedRule(), With(Data(index: new() { [page.NormalizedUrl] = new IndexInfo("PASS", "Gönderildi ve dizine eklendi") })), page).ShouldBeEmpty();
        var noindex = Page(Good with { MetaRobots = "noindex" }, path: "/c");
        Run(new NotIndexedRule(), With(Data(index: new() { [noindex.NormalizedUrl] = new IndexInfo("NEUTRAL", "noindex") })), noindex).ShouldBeEmpty();
    }

    [Fact]
    public void Web_vital_rules_use_thresholds_per_sample_and_device()
    {
        var type = Guid.CreateVersion7();
        var vitals = new List<VitalsSample>
        {
            new("https://example.com/a", type, DeviceStrategy.Mobile, 4100, 180, 0.31, 900),
            new("https://example.com/a", type, DeviceStrategy.Desktop, 1900, 90, 0.02, 400)
        };
        var lcp = Run(new LcpRule(), With(Data(vitals: vitals))).ShouldHaveSingleItem();
        lcp.CurrentValue.ShouldBe("4.100 ms (mobil)");
        lcp.PageTypeId.ShouldBe(type);
        Run(new ClsRule(), With(Data(vitals: vitals))).ShouldHaveSingleItem().CurrentValue.ShouldBe("0,31 (mobil)");
        Run(new InpRule(), With(Data(vitals: vitals))).ShouldBeEmpty();
        Run(new LcpRule(), With(Data(vitals: vitals), new Dictionary<string, decimal> { ["maxMs"] = 5000 })).ShouldBeEmpty();
    }

    [Fact]
    public void External_rules_declare_their_data_source()
    {
        new OpportunityRule().DataSource.ShouldBe(RuleDataSource.SearchConsole);
        new NotIndexedRule().DataSource.ShouldBe(RuleDataSource.UrlInspection);
        new LcpRule().DataSource.ShouldBe(RuleDataSource.PageSpeed);
        new TitleMissingRule().DataSource.ShouldBe(RuleDataSource.Crawl);
    }
}
