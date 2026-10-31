using System.Net.Http.Json;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Commands.RenderCrawl;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Rendering;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Rendering;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Rendering;

/// <summary>Tarama → render → değerlendirme. Chromium yerine sahte ölçücü: ürün sayfaları mobilde taşar.</summary>
[Collection(IntegrationTestCollection.Name)]
public class RenderPipelineTests(IntegrationTestContext context)
{
    [Fact]
    public async Task Render_measures_samples_on_three_devices_and_opens_render_findings()
    {
        var auditor = new OverflowingProducts();
        var (client, factory, siteId, crawlId) = await CrawlAsync(auditor);

        await SendAsync(factory, new RenderCrawlCommand(crawlId));
        await SendAsync(factory, new RenderCrawlCommand(crawlId));     // tekrar çalışsa da ikinci kez ölçmez
        await SendAsync(factory, new EvaluateCrawlCommand(crawlId));

        // Ana sayfa + her sayfa tipinden en fazla 3 örnek, her biri üç cihazda; bir kez.
        auditor.Calls.ShouldBe(1);
        auditor.Targets.GroupBy(t => t.Url).ShouldAllBe(g => g.Count() == 3);
        auditor.Targets.Count(t => t.Device == RenderDevice.Mobile && t.Url.Contains("/urun/")).ShouldBe(3);

        var overflow = (await client.GetFromJsonAsync<PagedResponse<FindingListItemResponse>>(
            $"/api/sites/{siteId}/findings?ruleCode=mobile.overflow&status=Open", IntegrationTestContext.Json))!.Items.ShouldHaveSingleItem();
        overflow.PageTypeName.ShouldBe("Ürün");
        overflow.AffectedCount.ShouldBe(3);
        overflow.Category.ShouldBe(FindingCategory.Accessibility);

        var detail = await client.GetStringAsync($"/sites/{siteId}/crawls/{crawlId}");
        detail.ShouldContain("ölçüm");
        var settings = await client.GetStringAsync($"/sites/{siteId}/settings/sites");
        settings.ShouldContain("Örnek sayfaları tarayıcıda ölç");
    }

    [Fact]
    public async Task A_failing_browser_does_not_fail_the_crawl_and_render_rules_do_not_run()
    {
        var (client, factory, siteId, crawlId) = await CrawlAsync(new BrokenBrowser());

        await SendAsync(factory, new RenderCrawlCommand(crawlId));
        await SendAsync(factory, new EvaluateCrawlCommand(crawlId));

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var crawl = await db.Crawls.AsNoTracking().SingleAsync(c => c.Id == crawlId);
        crawl.IsRendered.ShouldBeTrue();
        crawl.EvaluatedAt.ShouldNotBeNull();
        (await db.Findings.AnyAsync(f => f.SiteId == siteId && f.RuleCode == "mobile.overflow")).ShouldBeFalse();
        (await db.Findings.AnyAsync(f => f.SiteId == siteId && f.RuleCode == "mobile.viewport.invalid")).ShouldBeTrue();
    }

    private async Task<(HttpClient Client, Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> Factory, Guid SiteId, Guid CrawlId)> CrawlAsync(IRenderAuditor auditor)
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<ICrawlerHttpClient>(site);
            s.AddSingleton(auditor);
        }));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);
        await factory.Services.GetRequiredService<SiteCrawler>().RunAsync(crawlId, CancellationToken.None);
        return (client, factory, siteId, crawlId);
    }

    private static async Task SendAsync(Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory, IRequest request)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
    }

    private sealed class OverflowingProducts : IRenderAuditor
    {
        public int Calls { get; private set; }
        public List<RenderTarget> Targets { get; } = [];

        public Task<IReadOnlyList<RenderResult>> AuditAsync(IReadOnlyList<RenderTarget> targets, RenderOptions options, CancellationToken cancellationToken)
        {
            Calls++;
            Targets.AddRange(targets);
            IReadOnlyList<RenderResult> results = targets.Select(t => new RenderResult(t, t.Device == RenderDevice.Mobile && t.Url.Contains("/urun/")
                ? new RenderMeasurements { ViewportWidth = 375, DocumentWidth = 520, OverflowOffenders = [new RenderElement("table.ebatlar", null, 520)] }
                : new RenderMeasurements { ViewportWidth = 375, DocumentWidth = 375 }, false, null)).ToList();
            return Task.FromResult(results);
        }
    }

    private sealed class BrokenBrowser : IRenderAuditor
    {
        public Task<IReadOnlyList<RenderResult>> AuditAsync(IReadOnlyList<RenderTarget> targets, RenderOptions options, CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Chromium açılamadı.");
    }
}
