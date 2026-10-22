using System.Net;
using MarketingOps.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace MarketingOps.IntegrationTests.Web;

[Collection(IntegrationTestCollection.Name)]
public class ShellTests(IntegrationTestContext context)
{
    private readonly HttpClient _client = context.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Root_redirects_to_a_site_overview_that_renders_with_real_sites()
    {
        var redirect = await _client.GetAsync("/");
        redirect.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var location = redirect.Headers.Location!.ToString();
        location.ShouldMatch(@"^/sites/[0-9a-f-]{36}/overview$");

        var page = await _client.GetStringAsync(location);

        page.ShouldContain("Genel Bakış");
        page.ShouldContain("lastikborsasi.com");
        page.ShouldContain("tyresupply.com");
    }

    [Fact]
    public async Task Unknown_site_in_url_is_404()
    {
        var response = await _client.GetAsync($"/sites/{Guid.NewGuid()}/findings");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Design_page_renders_in_development() =>
        (await _client.GetAsync("/_design")).StatusCode.ShouldBe(HttpStatusCode.OK);
}
