using System.Net.Http.Json;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Domain.Findings;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Findings;

/// <summary>Faz 8 HTML denetimi: sahte sitenin eksik işaretlemesi taramada bulgu açar, düzelince çözülür.</summary>
[Collection(IntegrationTestCollection.Name)]
public class MarkupFindingTests(IntegrationTestContext context)
{
    [Fact]
    public async Task Missing_markup_opens_findings_per_page_type_and_resolves_when_fixed()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);

        async Task CrawlAndEvaluate()
        {
            var crawlId = await TestSites.StartCrawlAsync(client, siteId);
            await factory.Services.GetRequiredService<SiteCrawler>().RunAsync(crawlId, CancellationToken.None);
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EvaluateCrawlCommand(crawlId));
        }

        async Task<IReadOnlyList<FindingListItemResponse>> Findings(string ruleCode) =>
            (await client.GetFromJsonAsync<PagedResponse<FindingListItemResponse>>(
                $"/api/sites/{siteId}/findings?ruleCode={ruleCode}&status=Open&status=Reopened&status=Resolved", IntegrationTestContext.Json))!.Items;

        // Sahte sitenin sayfalarında viewport, lang, bölgeler ve paylaşım etiketleri yok; ürünlerin JSON-LD'si yalnızca @type.
        await CrawlAndEvaluate();

        var viewport = await Findings("mobile.viewport.invalid");
        viewport.ShouldContain(f => f.PageTypeName == "Ürün" && f.AffectedCount == 12 && f.Status == FindingStatus.Open);
        viewport.ShouldAllBe(f => f.Category == FindingCategory.Accessibility);
        (await Findings("a11y.lang.missing")).ShouldNotBeEmpty();
        (await Findings("a11y.landmarks.missing")).ShouldNotBeEmpty();
        (await Findings("social.og.incomplete")).ShouldNotBeEmpty();
        (await Findings("seo.schema.properties")).ShouldContain(f => f.PageTypeName == "Ürün" && f.AffectedCount == 12);
        (await Findings("seo.schema.site_identity")).ShouldHaveSingleItem();

        // Sıkılık: beklenen schema tanımlanmamış sayfa tipinde de structured data zorunlu.
        (await Findings("seo.schema.missing")).ShouldContain(f => f.PageTypeName == "Kategori");
        (await Findings("seo.schema.missing")).ShouldNotContain(f => f.PageTypeName == "Ürün");

        var overview = await client.GetStringAsync($"/sites/{siteId}/overview");
        overview.ShouldContain("Erişilebilirlik ve Mobil Sağlığı");

        // Ürün şablonuna viewport eklendi: Ürün tipindeki bulgu çözülür, diğer tiplerdeki açık kalır.
        site.SetProductHead("""<meta name="viewport" content="width=device-width, initial-scale=1">""");
        await CrawlAndEvaluate();

        var after = await Findings("mobile.viewport.invalid");
        after.Single(f => f.PageTypeName == "Ürün").Status.ShouldBe(FindingStatus.Resolved);
        after.ShouldContain(f => f.PageTypeName != "Ürün" && f.Status == FindingStatus.Open);
    }

    [Fact]
    public async Task Snapshots_from_before_the_markup_parser_open_no_markup_findings()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        site.SetProductTitles("Aynı ürün başlığı");
        var crawlId = await TestSites.StartCrawlAsync(client, siteId);
        await factory.Services.GetRequiredService<SiteCrawler>().RunAsync(crawlId, CancellationToken.None);

        // Faz 8'den önce yapılmış tarama: sayfa kayıtlarında işaretleme alanları yok (sürüm 0). "Kuralları çalıştır" böyle bir taramayı değerlendirir.
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.ExecuteSqlInterpolatedAsync($"update page_snapshots set data = jsonb_set(data, '{{version}}', '0') where crawl_id = {crawlId}");
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EvaluateCrawlCommand(crawlId));
        }

        var codes = (await client.GetFromJsonAsync<PagedResponse<FindingListItemResponse>>(
            $"/api/sites/{siteId}/findings?status=Open&pageSize=100", IntegrationTestContext.Json))!.Items.Select(f => f.RuleCode).ToList();
        codes.ShouldContain("seo.title.duplicate");                // eski kurallar çalışır
        codes.ShouldNotContain("seo.schema.missing");               // işaretleme kuralları eski kayıtta sessiz
        codes.ShouldNotContain("a11y.landmarks.missing");
        codes.ShouldNotContain("social.og.incomplete");
    }
}
