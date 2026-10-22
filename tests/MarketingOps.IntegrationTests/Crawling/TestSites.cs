using System.Net;
using System.Net.Http.Json;

namespace MarketingOps.IntegrationTests.Crawling;

/// <summary>Sahte siteyi API üzerinden kurar (sayfa tipleriyle) ve tarama başlatır.</summary>
public static class TestSites
{
    public static async Task<Guid> CreateAsync(HttpClient client, string domain, int maxPages = 1000)
    {
        var siteId = await (await client.PostAsJsonAsync("/api/sites", new { name = "Sahte mağaza", domain })).Content.ReadFromJsonAsync<Guid>();
        await client.PutAsJsonAsync($"/api/sites/{siteId}/crawl-settings",
            new { schedule = "Daily", maxRequestsPerSecond = 10, maxPages, excludedPathPatterns = new[] { "/arama/**" } });
        await AddPageType(client, siteId, "Kategori", "Any", ("UrlGlob", "/kategori/*"));
        await AddPageType(client, siteId, "Ürün", "All", ("UrlGlob", "/urun/*"), ("SchemaType", "Product"));
        await AddPageType(client, siteId, "Blog", "Any", ("UrlGlob", "/blog/*"));
        return siteId;
    }

    public static Task AddPageType(HttpClient client, Guid siteId, string name, string mode, params (string Type, string Value)[] conditions) =>
        client.PostAsJsonAsync($"/api/sites/{siteId}/page-types",
            new { name, mode, conditions = conditions.Select(c => new { type = c.Type, value = c.Value }) });

    public static async Task<Guid> StartCrawlAsync(HttpClient client, Guid siteId)
    {
        var response = await client.PostAsync($"/api/sites/{siteId}/crawls", null);
        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        return await response.Content.ReadFromJsonAsync<Guid>();
    }
}
