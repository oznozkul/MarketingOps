using System.Net;
using System.Net.Http.Json;
using System.Web;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Connectors.Commands.DisconnectConnector;
using MarketingOps.Application.Connectors.Commands.SelectConnectorProperty;
using MarketingOps.Application.Connectors.Commands.StartGoogleConnect;
using MarketingOps.Application.Connectors.Commands.SyncGa4;
using MarketingOps.Application.Connectors.Commands.SyncSearchConsole;
using MarketingOps.Application.Connectors.Commands.SyncWebVitals;
using MarketingOps.Application.Connectors.Queries.GetConnectors;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Sites.Queries.GetSiteById;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Findings;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Connectors;

[Collection(IntegrationTestCollection.Name)]
public class GoogleConnectorTests(IntegrationTestContext context)
{
    private sealed record Scenario(WebApplicationFactory<Program> Factory, HttpClient Client, SiteCrawler Crawler, FakeSite Site, FakeGoogle Google, Guid SiteId)
    {
        public async Task<T> SendAsync<T>(IRequest<T> request)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            return await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        }

        public async Task SendAsync(IRequest request)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        }

        public async Task CrawlAndEvaluateAsync()
        {
            var crawlId = await TestSites.StartCrawlAsync(Client, SiteId);
            await Crawler.RunAsync(crawlId, CancellationToken.None);
            await SendAsync(new EvaluateCrawlCommand(crawlId));
        }

        public async Task<IReadOnlyList<FindingListItemResponse>> FindingsAsync(string ruleCode) =>
            (await Client.GetFromJsonAsync<PagedResponse<FindingListItemResponse>>(
                $"/api/sites/{SiteId}/findings?ruleCode={ruleCode}&status=Open&status=Reopened&status=Resolved", IntegrationTestContext.Json))!.Items;

        /// <summary>Kullanıcının Google'a gidip izin verip dönmesi.</summary>
        public async Task<HttpResponseMessage> ConnectAsync(string? error = null)
        {
            var url = await SendAsync(new StartGoogleConnectCommand(SiteId, "http://localhost/connectors/google/callback"));
            var state = HttpUtility.ParseQueryString(new Uri(url).Query)["state"]!;
            var noRedirect = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
            return await noRedirect.GetAsync($"/connectors/google/callback?state={Uri.EscapeDataString(state)}" + (error is null ? "&code=abc" : $"&error={error}"));
        }
    }

    private async Task<Scenario> ArrangeAsync()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var google = new FakeGoogle(site.Domain, TimeProvider.System);
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<ICrawlerHttpClient>(site);
            s.AddSingleton<IGoogleAuthClient>(google);
            s.AddSingleton<ISearchConsoleClient>(google);
            s.AddSingleton<IGa4Client>(google);
            s.AddSingleton<IPageSpeedClient>(google);
        }));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        return new Scenario(factory, client, factory.Services.GetRequiredService<SiteCrawler>(), site, google, siteId);
    }

    [Fact]
    public async Task Connecting_google_and_selecting_the_matching_property_verifies_ownership()
    {
        var s = await ArrangeAsync();

        var callback = await s.ConnectAsync();
        callback.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        callback.Headers.Location!.ToString().ShouldBe($"/sites/{s.SiteId}/settings/connectors/google/select");

        var connectors = await s.SendAsync(new GetConnectorsQuery(s.SiteId));
        connectors.Connectors.Single(c => c.Type == ConnectorType.SearchConsole).Status.ShouldBe(ConnectorStatus.PendingApproval);
        connectors.Connectors.Single(c => c.Type == ConnectorType.Gtm).Status.ShouldBe(ConnectorStatus.Connected);

        var page = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/connectors/google/select");
        page.ShouldContain($"{s.Site.Domain} ile eşleşiyor");
        page.ShouldContain(FakeGoogle.Email);

        // Google'ın listesinde olmayan mülk reddedilir.
        await Should.ThrowAsync<MarketingOps.Domain.Common.DomainException>(() =>
            s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, "sc-domain:uydurma.com", null)));

        await s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, s.Google.SiteProperty, s.Google.Ga4PropertyId));
        (await s.Client.GetFromJsonAsync<SiteDetailResponse>($"/api/sites/{s.SiteId}", IntegrationTestContext.Json))!.IsOwnershipVerified.ShouldBeTrue();
        connectors = await s.SendAsync(new GetConnectorsQuery(s.SiteId));
        connectors.Connectors.Single(c => c.Type == ConnectorType.Ga4).ExternalAccountName.ShouldBe("Örnek Mağaza › Web");

        await using var scope = s.Factory.Services.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<AppDbContext>().ConnectorAccounts.FirstAsync(a => a.SiteId == s.SiteId);
        stored.ProtectedSecret.ShouldNotBeNull();
        stored.ProtectedSecret.ShouldNotContain("refresh-abc");
    }

    [Fact]
    public async Task Selected_properties_can_be_changed_and_removed()
    {
        var s = await ArrangeAsync();
        await s.ConnectAsync();
        await s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, s.Google.SiteProperty, s.Google.Ga4PropertyId));

        // Yalnızca GA4 kaldırılır; Search Console ve sahiplik olduğu gibi kalır (null = değişmez).
        await s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, null, ""));
        var connectors = (await s.SendAsync(new GetConnectorsQuery(s.SiteId))).Connectors;
        connectors.Single(c => c.Type == ConnectorType.Ga4).ShouldSatisfyAllConditions(
            c => c.Status.ShouldBe(ConnectorStatus.PendingApproval), c => c.ExternalAccountId.ShouldBeNull());
        connectors.Single(c => c.Type == ConnectorType.SearchConsole).ExternalAccountId.ShouldBe(s.Google.SiteProperty);

        // Yeniden seçilebilir; ekranda "Değiştir" bağlantısı görünür.
        await s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, null, s.Google.Ga4PropertyId));
        (await s.SendAsync(new GetConnectorsQuery(s.SiteId))).Connectors.Single(c => c.Type == ConnectorType.Ga4).Status.ShouldBe(ConnectorStatus.Connected);
        var settings = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/sites");
        settings.ShouldContain("google/select#ga4");
        settings.ShouldContain("Hesabı değiştir");
        (await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/connectors/google/select")).ShouldContain("Search Console bağlama");
    }

    [Fact]
    public async Task Denied_consent_returns_to_settings_without_connecting()
    {
        var s = await ArrangeAsync();
        var callback = await s.ConnectAsync(error: "access_denied");
        callback.Headers.Location!.ToString().ShouldBe($"/sites/{s.SiteId}/settings/sites#baglantilar");
        (await s.SendAsync(new GetConnectorsQuery(s.SiteId))).Connectors.ShouldAllBe(c => c.Status == ConnectorStatus.NotConnected);
    }

    [Fact]
    public async Task Synced_data_feeds_new_rules_and_missing_data_never_resolves_findings()
    {
        var s = await ArrangeAsync();
        await s.ConnectAsync();
        await s.SendAsync(new SelectConnectorPropertyCommand(s.SiteId, s.Google.SiteProperty, s.Google.Ga4PropertyId));
        await s.CrawlAndEvaluateAsync();   // örnek sayfalar ve URL Inspection adayları son taramadan gelir

        // Tekrar senkron yeni satır açmaz (upsert).
        await s.SendAsync(new SyncSearchConsoleCommand(s.SiteId));
        await s.SendAsync(new SyncSearchConsoleCommand(s.SiteId));
        await s.SendAsync(new SyncGa4Command(s.SiteId));
        await s.SendAsync(new SyncWebVitalsCommand(s.SiteId));
        await using (var scope = s.Factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            (await db.SearchPerformance.CountAsync(r => r.SiteId == s.SiteId)).ShouldBe(28 * 2);
            (await db.Ga4Events.CountAsync(r => r.SiteId == s.SiteId)).ShouldBe(2);
            (await db.Sitemaps.CountAsync(r => r.SiteId == s.SiteId)).ShouldBe(1);
            (await db.WebVitals.CountAsync(r => r.SiteId == s.SiteId)).ShouldBe(3 * 2 * 2); // 3 sayfa tipi × 2 örnek × 2 cihaz
            var inspected = await db.UrlIndexStatuses.CountAsync(r => r.SiteId == s.SiteId);
            inspected.ShouldBeGreaterThan(0);
            s.Google.InspectionCount.ShouldBe(inspected); // ikinci senkron aynı URL'leri yeniden denetlemedi
        }

        await s.CrawlAndEvaluateAsync();
        var opportunity = (await s.FindingsAsync("seo.opportunity.striking_distance")).ShouldHaveSingleItem();
        opportunity.PageTypeName.ShouldBe("Ürün");
        (await s.FindingsAsync("seo.index.not_indexed")).ShouldHaveSingleItem().AffectedCount.ShouldBe(1);
        (await s.FindingsAsync("perf.cwv.lcp")).Count.ShouldBe(3);   // her sayfa tipi için bir bulgu
        (await s.FindingsAsync("perf.cwv.inp")).ShouldBeEmpty();

        // Search Console bağlantısı kesilince kural çalışmaz; bulgu "görülmedi" diye çözülmez.
        await s.SendAsync(new DisconnectConnectorCommand(s.SiteId, ConnectorType.SearchConsole));
        await s.CrawlAndEvaluateAsync();
        (await s.FindingsAsync("seo.opportunity.striking_distance")).ShouldHaveSingleItem().Status.ShouldBe(FindingStatus.Open);

        // Ekranlar
        var performance = await s.Client.GetStringAsync($"/sites/{s.SiteId}/performance");
        performance.ShouldContain("Core Web Vitals");
        performance.ShouldContain("4,2 sn");
        performance.ShouldContain("Zayıf");
        performance.ShouldContain("Kontrol önerisi");
        (await s.Client.GetStringAsync($"/sites/{s.SiteId}/performance?device=Desktop")).ShouldContain("Gerçek kullanıcı (site geneli)");

        var detail = await s.Client.GetStringAsync($"/sites/{s.SiteId}/findings/{opportunity.Id}");
        detail.ShouldContain("Ort. sıra");
        detail.ShouldContain("etkilenen sayfalardan 1 tanesinin verisi var");

        var settings = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/sites");
        settings.ShouldContain(FakeGoogle.Email);
        settings.ShouldContain("1 sitemap");
    }
}
