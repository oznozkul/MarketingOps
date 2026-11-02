using System.Net.Http.Json;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Sites.Commands.CompleteSetupStep;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Sites;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MarketingOps.IntegrationTests.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Web;

[Collection(IntegrationTestCollection.Name)]
public class SetupWizardTests(IntegrationTestContext context)
{
    [Fact]
    public async Task New_site_starts_the_wizard_and_resumes_from_the_step_it_was_left_on()
    {
        var client = context.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var page = await client.GetStringAsync((await client.GetAsync("/")).Headers.Location!.ToString());

        var created = await HtmxForm.PostAsync(client, page, "/sites/new", ("Name", "Sihirbaz mağaza"), ("Domain", UniqueDomain.Next()));
        var stepUrl = HtmxForm.Redirect(created);
        stepUrl.ShouldEndWith("/setup/SearchConsole");
        var setupBase = stepUrl[..stepUrl.LastIndexOf('/')];
        var siteBase = setupBase[..setupBase.LastIndexOf('/')];

        (await client.GetAsync(setupBase)).Headers.Location!.ToString().ShouldEndWith("/setup/SearchConsole");
        (await client.GetAsync($"{setupBase}/FirstCrawl")).Headers.Location!.ToString().ShouldEndWith("/setup/SearchConsole");
        (await HtmxForm.BodyAsync(await client.GetAsync($"{siteBase}/overview"))).ShouldContain("Bu sitenin kurulumu tamamlanmadı.");

        var completed = await HtmxForm.PostAsync(client, page, $"{setupBase}/SearchConsole/complete");
        HtmxForm.Redirect(completed).ShouldEndWith("/setup/Connectors");
        (await client.GetAsync(setupBase)).Headers.Location!.ToString().ShouldEndWith("/setup/Connectors");

        var skipped = await HtmxForm.PostAsync(client, page, $"{setupBase}/PageTypes/complete");
        HtmxForm.Trigger(skipped).ShouldContain("Önceki kurulum adımları");
    }

    [Fact]
    public async Task First_crawl_starts_without_verified_ownership_and_finishes_setup()
    {
        // Domain kuralı: doğrulanmamış sitede yalnızca zamanlanmış tarama engellenir; sihirbaz bunu uyarı olarak gösterir.
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var siteId = await CreateBareSiteAsync(client, site.Domain);
        foreach (var step in new[] { SiteSetupStep.SearchConsole, SiteSetupStep.Connectors, SiteSetupStep.PageTypes, SiteSetupStep.CrawlSettings })
            await SendAsync(factory, new CompleteSetupStepCommand(siteId, step));
        var page = await HtmxForm.BodyAsync(await client.GetAsync($"/sites/{siteId}/setup/FirstCrawl"));
        page.ShouldContain("düzenli taramalar");

        var start = await HtmxForm.PostAsync(client, page, $"/sites/{siteId}/setup/first-crawl");

        HtmxForm.Redirect(start).ShouldStartWith($"/sites/{siteId}/crawls/");
        (await SiteAsync(factory, siteId)).SetupStep.ShouldBeNull();
        (await client.GetAsync($"/sites/{siteId}/setup")).Headers.Location!.ToString().ShouldEndWith("/overview");
    }

    [Fact]
    public async Task Sitemap_suggestions_are_measured_against_real_urls_and_can_be_added()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var ai = new FakeAiSuggestionService();
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<ICrawlerHttpClient>(site);
            s.AddSingleton<IAiSuggestionService>(ai);
        }));
        var client = factory.CreateClient();
        var siteId = await CreateBareSiteAsync(client, site.Domain);

        var suggestions = await HtmxForm.BodyAsync(await client.GetAsync($"/sites/{siteId}/settings/page-types/suggestions?source=Sitemap"));

        var largest = ai.PageTypeRequests.ShouldHaveSingleItem().Groups[0];
        largest.Pattern.ShouldBe("/urun/**");
        largest.Count.ShouldBe(12);
        suggestions.ShouldContain("value=\"/urun/*\"");
        suggestions.ShouldNotContain("Hayalet");    // hiçbir sayfaya uymuyor
        suggestions.ShouldNotContain("Bozuk");      // geçersiz desen
        suggestions.ShouldNotContain("value=\"Kategori\""); // sitemap'te kategori adresi yok

        var page = await client.GetStringAsync($"/sites/{siteId}/setup/PageTypes");
        var added = await HtmxForm.PostAsync(client, page, $"/sites/{siteId}/settings/page-types/suggestions", ("name", "Ürün"), ("urlGlob", "/urun/*"));

        (await HtmxForm.BodyAsync(added)).ShouldContain("eklendi");
        (await SiteAsync(factory, siteId)).PageTypes.ShouldHaveSingleItem().Name.ShouldBe("Ürün");
    }

    [Fact]
    public async Task Without_ai_the_url_groups_are_offered_for_manual_naming()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);

        var suggestions = await HtmxForm.BodyAsync(await client.GetAsync($"/sites/{siteId}/settings/page-types/suggestions?source=Sitemap"));

        suggestions.ShouldContain("AI sağlayıcısı yapılandırılmadığı");
        suggestions.ShouldContain("value=\"/urun/**\"");
    }

    /// <summary>Sayfa tipi olmayan site; TestSites.CreateAsync "Ürün" tipini de eklediği için öneri aynı adla elenirdi.</summary>
    private static async Task<Guid> CreateBareSiteAsync(HttpClient client, string domain) =>
        await (await client.PostAsJsonAsync("/api/sites", new { name = "Sihirbaz mağaza", domain })).Content.ReadFromJsonAsync<Guid>();

    private static async Task SendAsync(WebApplicationFactory<Program> factory, IRequest request)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private static async Task<SiteDetailResponse> SiteAsync(WebApplicationFactory<Program> factory, Guid siteId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetSiteByIdQuery(siteId));
    }
}
