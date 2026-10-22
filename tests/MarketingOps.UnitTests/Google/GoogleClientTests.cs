using System.Net;
using MarketingOps.Application.Connectors.Google;
using MarketingOps.Domain.Metrics;
using MarketingOps.Infrastructure.Google;

namespace MarketingOps.UnitTests.Google;

/// <summary>Google yanıtlarının ayrıştırılması ve hata eşlemesi (gerçek ağ yok; yanıtlar Google belgelerindeki biçimde).</summary>
public class GoogleClientTests
{
    private static HttpClient Http(Func<HttpRequestMessage, (HttpStatusCode, string)> respond, out FakeHandler handler) =>
        new(handler = new FakeHandler(respond));

    [Fact]
    public async Task Search_console_pages_are_parsed_and_paged()
    {
        var http = Http(_ => (HttpStatusCode.OK, """
            {"rows":[{"keys":["2026-10-01","https://www.ornek.com/a"],"clicks":3,"impressions":120,"ctr":0.025,"position":12.4}]}
            """), out var handler);
        var rows = await new SearchConsoleClient(http).QueryPagesAsync("token", "sc-domain:ornek.com", new DateOnly(2026, 9, 4), new DateOnly(2026, 10, 1), CancellationToken.None);

        rows.ShouldHaveSingleItem().ShouldBe(new SearchPerformanceRow(new DateOnly(2026, 10, 1), "https://www.ornek.com/a", 3, 120, 0.025, 12.4));
        handler.Requests[0].Request.RequestUri!.AbsoluteUri.ShouldContain("sites/sc-domain%3Aornek.com/searchAnalytics/query");
        handler.Requests[0].Request.Headers.Authorization!.Parameter.ShouldBe("token");
        handler.Requests[0].Body!.ShouldContain("\"dimensions\":[\"date\",\"page\"]");
    }

    [Fact]
    public async Task Unauthorized_is_an_authorization_error()
    {
        var http = Http(_ => (HttpStatusCode.Unauthorized, """{"error":{"message":"Invalid Credentials"}}"""), out _);
        var ex = await Should.ThrowAsync<GoogleApiException>(() => new SearchConsoleClient(http).ListSitesAsync("x", CancellationToken.None));
        ex.IsAuthorizationError.ShouldBeTrue();
    }

    [Fact]
    public async Task Ga4_report_rows_are_parsed()
    {
        var http = Http(_ => (HttpStatusCode.OK, """
            {"rows":[{"dimensionValues":[{"value":"20261020"},{"value":"purchase"}],"metricValues":[{"value":"41"},{"value":"38250.5"}]}]}
            """), out var handler);
        var rows = await new Ga4Client(http).RunEventReportAsync("t", "properties/9", new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 20), ["purchase"], CancellationToken.None);
        rows.ShouldHaveSingleItem().ShouldBe(new Ga4EventRow(new DateOnly(2026, 10, 20), "purchase", 41, 38250.5m));
        handler.Requests[0].Request.RequestUri!.AbsoluteUri.ShouldEndWith("/v1beta/properties/9:runReport");
    }

    [Fact]
    public async Task Page_speed_prefers_page_field_data_then_origin_then_lab()
    {
        var page = await RunPageSpeed("""
            {"loadingExperience":{"metrics":{"LARGEST_CONTENTFUL_PAINT_MS":{"percentile":2900},"INTERACTION_TO_NEXT_PAINT":{"percentile":210},"CUMULATIVE_LAYOUT_SHIFT_SCORE":{"percentile":12},"EXPERIMENTAL_TIME_TO_FIRST_BYTE":{"percentile":950}}}}
            """);
        page.ShouldBe(new PageSpeedResult(VitalsSource.UrlField, 2900, 210, 0.12, 950));

        var origin = await RunPageSpeed("""
            {"loadingExperience":{},"originLoadingExperience":{"metrics":{"LARGEST_CONTENTFUL_PAINT_MS":{"percentile":2100}}}}
            """);
        origin.Source.ShouldBe(VitalsSource.OriginField);

        var lab = await RunPageSpeed("""
            {"lighthouseResult":{"audits":{"largest-contentful-paint":{"numericValue":3456.7},"cumulative-layout-shift":{"numericValue":0.05},"server-response-time":{"numericValue":612}}}}
            """);
        lab.ShouldBe(new PageSpeedResult(VitalsSource.Lab, 3457, null, 0.05, 612));
    }

    private static Task<PageSpeedResult> RunPageSpeed(string body) =>
        new PageSpeedClient(Http(_ => (HttpStatusCode.OK, body), out _), new GoogleOptions()).RunAsync("https://ornek.com/", DeviceStrategy.Mobile, CancellationToken.None);

    [Fact]
    public void Authorization_url_requests_offline_read_only_access()
    {
        var url = new GoogleAuthClient(new HttpClient(), new GoogleOptions { ClientId = "cid", ClientSecret = "s" })
            .BuildAuthorizationUrl("durum", "http://localhost:8080/connectors/google/callback");
        url.ShouldStartWith("https://accounts.google.com/o/oauth2/v2/auth?");
        url.ShouldContain("access_type=offline");
        url.ShouldContain("prompt=consent");
        url.ShouldContain(Uri.EscapeDataString("https://www.googleapis.com/auth/webmasters.readonly"));
        url.ShouldNotContain("webmasters%20"); // yazma izni (webmasters, scope sonu) istenmez
        url.ShouldContain("state=durum");
    }
}
