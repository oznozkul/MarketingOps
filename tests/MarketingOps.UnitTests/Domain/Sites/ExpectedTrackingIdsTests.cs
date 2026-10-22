using MarketingOps.Domain.Common;
using MarketingOps.Domain.Sites;

namespace MarketingOps.UnitTests.Domain.Sites;

public class ExpectedTrackingIdsTests
{
    [Fact]
    public void Valid_ids_are_accepted_and_normalized()
    {
        var ids = new ExpectedTrackingIds("g-lb7q2k9x1m", " GTM-K8X2LQ7 ", "418273645509812", "aw-11223344556");

        ids.Ga4MeasurementId.ShouldBe("G-LB7Q2K9X1M");
        ids.GtmContainerId.ShouldBe("GTM-K8X2LQ7");
        ids.MetaPixelId.ShouldBe("418273645509812");
        ids.GoogleAdsConversionId.ShouldBe("AW-11223344556");
    }

    [Fact]
    public void Blank_values_become_null()
    {
        var ids = new ExpectedTrackingIds("", null, "  ", null);

        ids.ShouldBe(ExpectedTrackingIds.None);
    }

    [Theory]
    [InlineData("UA-12345-1", null, null, null)]
    [InlineData(null, "K8X2LQ7", null, null)]
    [InlineData(null, null, "abc", null)]
    [InlineData(null, null, null, "11223344556")]
    public void Wrong_formats_are_rejected(string? ga4, string? gtm, string? pixel, string? ads) =>
        Should.Throw<DomainException>(() => new ExpectedTrackingIds(ga4, gtm, pixel, ads));
}
