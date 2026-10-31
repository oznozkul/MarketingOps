using System.Net;
using System.Net.Http.Json;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Application.Sites.Queries.GetSites;
using MarketingOps.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.IntegrationTests.Api;

[Collection(IntegrationTestCollection.Name)]
public class SitesApiTests(IntegrationTestContext context)
{
    private readonly HttpClient _client = context.Factory.CreateClient();

    private async Task<Guid> CreateSiteAsync(string domain, string name = "Test sitesi")
    {
        var response = await _client.PostAsJsonAsync("/api/sites", new { name, domain });
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    [Fact]
    public async Task Site_and_page_type_lifecycle()
    {
        var domain = UniqueDomain.Next();
        var siteId = await CreateSiteAsync($"https://www.{domain.ToUpperInvariant()}/");

        var site = await _client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{siteId}", IntegrationTestContext.Json);
        site!.Domain.ShouldBe(domain);
        site.CrawlSettings.RespectRobotsTxt.ShouldBeTrue();

        (await _client.PutAsJsonAsync($"/api/sites/{siteId}", new { name = "Yeni ad" })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.PutAsJsonAsync($"/api/sites/{siteId}/crawl-settings",
            new { schedule = "Weekly", maxRequestsPerSecond = 1, maxPages = 500, excludedPathPatterns = new[] { "/hesabim/**" } }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.PutAsJsonAsync($"/api/sites/{siteId}/expected-tracking-ids",
            new { ga4MeasurementId = "G-ABC1234567", gtmContainerId = "GTM-K8X2LQ7" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var add = await _client.PostAsJsonAsync($"/api/sites/{siteId}/page-types", new
        {
            name = "Ürün",
            mode = "All",
            conditions = new object[] { new { type = "UrlGlob", value = "/urun/*" }, new { type = "SchemaType", value = "Product" } }
        });
        add.StatusCode.ShouldBe(HttpStatusCode.Created);
        var pageTypeId = await add.Content.ReadFromJsonAsync<Guid>();

        (await _client.PutAsJsonAsync($"/api/sites/{siteId}/page-types/{pageTypeId}", new
        {
            name = "Ürün detay",
            mode = "Any",
            conditions = new object[] { new { type = "UrlRegex", value = "^/urun/[a-z0-9-]+$" } }
        })).StatusCode.ShouldBe(HttpStatusCode.NoContent);

        site = await _client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{siteId}", IntegrationTestContext.Json);
        site!.Name.ShouldBe("Yeni ad");
        site.CrawlSettings.MaxPages.ShouldBe(500);
        site.CrawlSettings.ExcludedPathPatterns.ShouldBe(["/hesabim/**"]);
        site.ExpectedTrackingIds.GtmContainerId.ShouldBe("GTM-K8X2LQ7");
        var pageType = site.PageTypes.ShouldHaveSingleItem();
        pageType.Name.ShouldBe("Ürün detay");
        pageType.Conditions.ShouldHaveSingleItem().Value.ShouldBe("^/urun/[a-z0-9-]+$");

        (await _client.DeleteAsync($"/api/sites/{siteId}/page-types/{pageTypeId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.DeleteAsync($"/api/sites/{siteId}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/sites/{siteId}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Render_settings_default_to_on_and_are_saved_and_validated()
    {
        var siteId = await CreateSiteAsync(UniqueDomain.Next());
        var settings = $"/api/sites/{siteId}/crawl-settings";

        var created = (await _client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{siteId}", IntegrationTestContext.Json))!.CrawlSettings;
        created.RenderEnabled.ShouldBeTrue();
        created.RenderSamplesPerPageType.ShouldBe(3);

        (await _client.PutAsJsonAsync(settings, new { schedule = "Daily", maxRequestsPerSecond = 2, maxPages = 100, excludedPathPatterns = Array.Empty<string>(),
            renderEnabled = false, renderSamplesPerPageType = 5 })).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var saved = (await _client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{siteId}", IntegrationTestContext.Json))!.CrawlSettings;
        saved.RenderEnabled.ShouldBeFalse();
        saved.RenderSamplesPerPageType.ShouldBe(5);

        var invalid = await _client.PutAsJsonAsync(settings, new { schedule = "Daily", maxRequestsPerSecond = 2, maxPages = 100, excludedPathPatterns = Array.Empty<string>(),
            renderSamplesPerPageType = 0 });
        invalid.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadFromJsonAsync<ValidationProblemDetails>())!.Errors.Keys.ShouldContain(k => k.Contains("RenderSamplesPerPageType", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Same_domain_written_differently_is_a_conflict()
    {
        var domain = UniqueDomain.Next();
        await CreateSiteAsync(domain);

        var response = await _client.PostAsJsonAsync("/api/sites", new { name = "Kopya", domain = $"http://WWW.{domain}/kategori" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe($"{domain} için zaten bir site kayıtlı.");
    }

    [Fact]
    public async Task Unknown_site_returns_404_problem()
    {
        var response = await _client.GetAsync($"/api/sites/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Title.ShouldBe("Site bulunamadı.");
    }

    [Fact]
    public async Task Invalid_input_returns_field_errors()
    {
        var response = await _client.PostAsJsonAsync("/api/sites", new { name = "", domain = "" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        problem!.Errors.Keys.ShouldBe(["Name", "Domain"], ignoreOrder: true);
        problem.Errors["Name"].ShouldContain("Site adı zorunludur.");
    }

    [Fact]
    public async Task Domain_rule_violations_return_400()
    {
        var siteId = await CreateSiteAsync(UniqueDomain.Next());

        var badRegex = await _client.PostAsJsonAsync($"/api/sites/{siteId}/page-types", new
        {
            name = "Bozuk", mode = "Any", conditions = new[] { new { type = "UrlRegex", value = "([" } }
        });
        var badDomain = await _client.PostAsJsonAsync("/api/sites", new { name = "X", domain = "localhost" });

        badRegex.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await badRegex.Content.ReadFromJsonAsync<ProblemDetails>())!.Title!.ShouldStartWith("Geçersiz düzenli ifade");
        badDomain.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Removing_unknown_page_type_returns_404()
    {
        var siteId = await CreateSiteAsync(UniqueDomain.Next());

        var response = await _client.DeleteAsync($"/api/sites/{siteId}/page-types/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_is_paged()
    {
        var page = await _client.GetFromJsonAsync<PagedResponse<SiteListItemResponse>>("/api/sites?page=1&pageSize=1", IntegrationTestContext.Json);

        page!.Items.Count.ShouldBe(1);
        page.PageSize.ShouldBe(1);
        page.TotalCount.ShouldBeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Preview_does_not_persist_and_reports_matches()
    {
        var siteId = await CreateSiteAsync(UniqueDomain.Next());
        var response = await _client.PostAsJsonAsync($"/api/sites/{siteId}/page-types/preview", new
        {
            mode = "Any",
            conditions = new[] { new { type = "UrlGlob", value = "/lastik/*/*" } },
            samples = new[] { new { url = "https://a.com/lastik/205-55-r16/michelin" }, new { url = "https://a.com/lastik/yaz" } }
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"matchedCount\":1");
        (await _client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{siteId}", IntegrationTestContext.Json))!.PageTypes.ShouldBeEmpty();
    }

    [Fact]
    public async Task Preview_for_unknown_site_is_404()
    {
        var response = await _client.PostAsJsonAsync($"/api/sites/{Guid.NewGuid()}/page-types/preview", new
        {
            mode = "Any",
            conditions = new[] { new { type = "UrlGlob", value = "/a/*" } },
            samples = new[] { new { url = "https://a.com/a/b" } }
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Preview_reports_overlaps_with_other_page_types_of_the_site()
    {
        var siteId = await CreateSiteAsync(UniqueDomain.Next());
        await _client.PostAsJsonAsync($"/api/sites/{siteId}/page-types", new
        {
            name = "Ürün", mode = "Any", conditions = new[] { new { type = "UrlGlob", value = "/urun/*" } }
        });

        var response = await _client.PostAsJsonAsync($"/api/sites/{siteId}/page-types/preview", new
        {
            mode = "Any",
            conditions = new[] { new { type = "UrlGlob", value = "/urun/**" } },
            samples = new[] { new { url = "https://a.com/urun/michelin" }, new { url = "https://a.com/urun/a/b" } }
        });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("\"overlapCount\":1");
        body.ShouldContain("\"overlappingPageTypes\":[\"Ürün\"]");
    }
}
