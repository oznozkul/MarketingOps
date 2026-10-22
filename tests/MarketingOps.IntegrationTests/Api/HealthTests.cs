using MarketingOps.IntegrationTests.Infrastructure;

namespace MarketingOps.IntegrationTests.Api;

[Collection(IntegrationTestCollection.Name)]
public class HealthTests(IntegrationTestContext context)
{
    [Fact]
    public async Task Health_endpoint_reports_healthy()
    {
        var response = await context.Factory.CreateClient().GetAsync("/health");

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task OpenApi_document_lists_site_endpoints()
    {
        var document = await context.Factory.CreateClient().GetStringAsync("/openapi/v1.json");

        document.ShouldContain("/api/sites");
        document.ShouldContain("/api/sites/{siteId}/page-types");
    }
}
