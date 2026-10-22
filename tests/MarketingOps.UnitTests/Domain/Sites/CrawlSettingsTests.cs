using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Sites;

public class CrawlSettingsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void Rate_limit_outside_range_is_rejected(int rps) =>
        Should.Throw<DomainException>(() => new CrawlSettings(CrawlSchedule.Daily, rps, 1000, []));

    [Theory]
    [InlineData(0)]
    [InlineData(CrawlSettings.MaxPagesLimit + 1)]
    public void Max_pages_outside_range_is_rejected(int maxPages) =>
        Should.Throw<DomainException>(() => new CrawlSettings(CrawlSchedule.Daily, 2, maxPages, []));

    [Fact]
    public void Excluded_patterns_are_trimmed_and_deduplicated()
    {
        var settings = new CrawlSettings(CrawlSchedule.Weekly, 1, 500, [" /hesabim/** ", "/HESABIM/**", "", "/arama?*"]);

        settings.ExcludedPathPatterns.ShouldBe(["/hesabim/**", "/arama?*"]);
    }

    [Fact]
    public void Robots_txt_is_always_respected() =>
        CrawlSettings.Default.RespectRobotsTxt.ShouldBeTrue();

    [Fact]
    public void Default_returns_a_new_instance_each_time() =>
        ReferenceEquals(CrawlSettings.Default, CrawlSettings.Default).ShouldBeFalse();
}
