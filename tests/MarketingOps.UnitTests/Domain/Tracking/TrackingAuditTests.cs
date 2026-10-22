using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Domain.Sites;
using MarketingOps.Domain.Tracking;

namespace MarketingOps.UnitTests.Domain.Tracking;

public class TrackingAuditTests
{
    [Fact]
    public void Classifier_names_gtm_ga4_ads_and_meta_and_ignores_the_site()
    {
        var gtm = TagRequestClassifier.Classify("https://www.googletagmanager.com/gtm.js?id=GTM-ABCD", false);
        gtm.ShouldNotBeNull();
        gtm!.Vendor.ShouldBe(TagVendor.Gtm);
        gtm.Id.ShouldBe("GTM-ABCD");

        var ga4 = TagRequestClassifier.Classify("https://www.google-analytics.com/g/collect?tid=G-CORRECT&gcs=G100&gcd=13", true);
        ga4.ShouldNotBeNull();
        ga4!.Vendor.ShouldBe(TagVendor.Ga4);
        ga4.Id.ShouldBe("G-CORRECT");
        ga4.Gcs.ShouldBe("G100");
        ga4.Gcd.ShouldBe("13");
        ga4.BeforeConsent.ShouldBeTrue();

        TagRequestClassifier.Classify("https://www.googletagmanager.com/gtag/js?id=G-OTHER", false)!.Vendor.ShouldBe(TagVendor.Ga4);
        TagRequestClassifier.Classify("https://www.googletagmanager.com/gtag/js?id=AW-633945173", false)!.Vendor.ShouldBe(TagVendor.GoogleAds);
        TagRequestClassifier.Classify("https://www.googleadservices.com/pagead/conversion/123456/?label=x", false)!.Vendor.ShouldBe(TagVendor.GoogleAds);
        TagRequestClassifier.Classify("https://www.facebook.com/tr?id=1234567890", false)!.Vendor.ShouldBe(TagVendor.MetaPixel);
        TagRequestClassifier.Classify("https://example.com/sepet", false).ShouldBeNull();
    }

    [Fact]
    public void Status_prefers_prefire_then_wrong_id_and_skips_wrong_id_without_an_expected_id()
    {
        TagAuditBuilder.Status(null, [], false).ShouldBe(TagAuditStatus.Ok);
        TagAuditBuilder.Status("G-CORRECT", [], false).ShouldBe(TagAuditStatus.Missing);
        TagAuditBuilder.Status("G-CORRECT", ["G-A", "G-B"], false).ShouldBe(TagAuditStatus.WrongId);
        TagAuditBuilder.Status("G-CORRECT", ["G-CORRECT", "G-B"], false).ShouldBe(TagAuditStatus.Duplicate);
        TagAuditBuilder.Status("G-CORRECT", ["G-CORRECT"], true).ShouldBe(TagAuditStatus.FiresBeforeConsent);
        TagAuditBuilder.Status(null, ["G-CORRECT"], false).ShouldBe(TagAuditStatus.Ok);
    }

    [Fact]
    public void Gtm_before_consent_is_not_prefire()
    {
        var page = new AuditedPage("Ana sayfa", null, "https://ornek.com/",
            [new CapturedTagRequest("https://www.googletagmanager.com/gtm.js?id=GTM-ABCD", TagVendor.Gtm, "GTM-ABCD", null, null, true)]);
        var cells = TagAuditBuilder.Build(Guid.NewGuid(), [page], new ExpectedTrackingIds(null, "GTM-ABCD", null, null));
        cells.Single(c => c.Vendor == TagVendor.Gtm).Status.ShouldBe(TagAuditStatus.Ok);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 5, 100)]
    [InlineData(100, 90, 10)]
    [InlineData(100, 100, 0)]
    public void Gap_does_not_divide_by_zero(int orders, long purchases, int expected) =>
        OrderComparisonDaily.Gap(orders, purchases).ShouldBe(expected);

    [Fact]
    public void Gap_is_empty_when_either_side_is_missing() =>
        OrderComparisonDaily.Gap(10, null).ShouldBeNull();

    [Fact]
    public void Anomaly_flags_a_drop_or_a_fall_to_zero_and_ignores_a_flat_zero()
    {
        var rule = new EventAnomalyRule();
        var violations = rule.Evaluate("https://ornek.com/",
        [
            new EventDay("purchase", 0, 10),
            new EventDay("view_item", 40, 100),
            new EventDay("add_to_cart", 60, 100),
            new EventDay("search", 0, 0)
        ], 50).ToList();

        violations.Select(v => v.Detail).ShouldBe(["purchase: dün 0, 7 gün ortalaması 10,0", "view_item: dün 40, 7 gün ortalaması 100,0"]);
    }

    [Fact]
    public void Order_query_must_be_one_select_with_a_date_parameter()
    {
        var ok = new OrderSourceSettings("secret", "SELECT COUNT(*) AS order_count FROM orders WHERE day = @date");
        ok.IsConfigured.ShouldBeTrue();
        Should.Throw<MarketingOps.Domain.Common.DomainException>(() => new OrderSourceSettings("secret", "DELETE FROM orders WHERE day = @date"));
        Should.Throw<MarketingOps.Domain.Common.DomainException>(() => new OrderSourceSettings("secret", "SELECT 1; SELECT 2"));
        Should.Throw<MarketingOps.Domain.Common.DomainException>(() => new OrderSourceSettings("secret", "SELECT 1"));
    }
}
