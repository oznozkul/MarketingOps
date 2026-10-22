using MarketingOps.Application.Sites.Commands.CreateSite;
using MarketingOps.Application.Sites.Commands.UpdateCrawlSettings;
using MarketingOps.Application.Sites.Queries.GetSites;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Application.Sites;

public class SiteValidatorTests
{
    [Fact]
    public void Create_site_requires_name_and_domain()
    {
        var result = new CreateSiteCommandValidator().Validate(new CreateSiteCommand("", ""));

        result.Errors.Select(e => e.PropertyName).ShouldBe(["Name", "Domain"], ignoreOrder: true);
        result.Errors.ShouldAllBe(e => e.ErrorMessage.EndsWith("zorunludur."));
    }

    [Fact]
    public void Create_site_rejects_overlong_name() =>
        new CreateSiteCommandValidator().Validate(new CreateSiteCommand(new string('a', Site.NameMaxLength + 1), "a.com"))
            .IsValid.ShouldBeFalse();

    [Theory]
    [InlineData(0, 25)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public void Paging_limits_are_enforced(int page, int pageSize) =>
        new GetSitesQueryValidator().Validate(new GetSitesQuery(page, pageSize)).IsValid.ShouldBeFalse();

    [Fact]
    public void Crawl_settings_validator_reports_range_errors_in_turkish()
    {
        var result = new UpdateCrawlSettingsCommandValidator().Validate(
            new UpdateCrawlSettingsCommand(Guid.CreateVersion7(), CrawlSchedule.Daily, 50, 0, []));

        result.Errors.Select(e => e.PropertyName).ShouldBe(["MaxRequestsPerSecond", "MaxPages"], ignoreOrder: true);
        result.Errors.First(e => e.PropertyName == "MaxRequestsPerSecond").ErrorMessage.ShouldContain("istek/saniye");
    }
}
