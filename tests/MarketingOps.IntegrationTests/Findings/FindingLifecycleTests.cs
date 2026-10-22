using System.Net;
using System.Net.Http.Json;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Crawls.Queries;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MarketingOps.Application.Findings.Queries;
using MarketingOps.Application.Findings.Queries.GetFindingById;
using MarketingOps.Application.Rules.Queries.GetRule;
using MarketingOps.Application.Rules.Queries.PreviewRuleThresholdImpact;
using MarketingOps.Domain.Findings;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Findings;

[Collection(IntegrationTestCollection.Name)]
public class FindingLifecycleTests(IntegrationTestContext context)
{
    private const string TitleDuplicate = "seo.title.duplicate";

    private sealed record Scenario(WebApplicationFactory<Program> Factory, HttpClient Client, SiteCrawler Crawler, FakeSite Site, Guid SiteId)
    {
        /// <summary>Taramayı çalıştırır ve işçinin yapacağı gibi kural değerlendirmesini tetikler.</summary>
        public async Task<Guid> CrawlAndEvaluateAsync()
        {
            var crawlId = await TestSites.StartCrawlAsync(Client, SiteId);
            await Crawler.RunAsync(crawlId, CancellationToken.None);
            await using var scope = Factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EvaluateCrawlCommand(crawlId));
            return crawlId;
        }

        public async Task<IReadOnlyList<FindingListItemResponse>> FindingsAsync(string ruleCode) =>
            (await Client.GetFromJsonAsync<PagedResponse<FindingListItemResponse>>(
                $"/api/sites/{SiteId}/findings?ruleCode={ruleCode}&status=Open&status=Reopened&status=Resolved&status=FalsePositive&status=Snoozed",
                IntegrationTestContext.Json))!.Items;
    }

    private async Task<Scenario> ArrangeAsync()
    {
        var site = new FakeSite(UniqueDomain.Next());
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s => s.AddSingleton<ICrawlerHttpClient>(site)));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        return new Scenario(factory, client, factory.Services.GetRequiredService<SiteCrawler>(), site, siteId);
    }

    [Fact]
    public async Task Finding_opens_resolves_and_reopens_across_three_crawls_without_duplicates()
    {
        var s = await ArrangeAsync();

        // 1. tarama: 12 ürün aynı title'ı taşır → Ürün tipinde tek bulgu açılır.
        s.Site.SetProductTitles("Aynı ürün başlığı");
        var first = await s.CrawlAndEvaluateAsync();
        var opened = (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem();
        opened.Status.ShouldBe(FindingStatus.Open);
        opened.PageTypeName.ShouldBe("Ürün");
        opened.AffectedCount.ShouldBe(12);
        opened.IsNewInLastCrawl.ShouldBeTrue();

        // 2. tarama: title'lar düzeltildi → aynı bulgu çözülür.
        s.Site.SetProductTitles(null);
        var second = await s.CrawlAndEvaluateAsync();
        var resolved = (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem();
        resolved.Id.ShouldBe(opened.Id);
        resolved.Status.ShouldBe(FindingStatus.Resolved);

        // 3. tarama: sorun geri geldi → yeni kayıt açılmaz, aynı bulgu tekrar açılır.
        s.Site.SetProductTitles("Aynı ürün başlığı");
        var third = await s.CrawlAndEvaluateAsync();
        var reopened = (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem();
        reopened.Id.ShouldBe(opened.Id);
        reopened.Status.ShouldBe(FindingStatus.Reopened);

        var detail = await s.Client.GetFromJsonAsync<FindingDetailResponse>($"/api/sites/{s.SiteId}/findings/{opened.Id}", IntegrationTestContext.Json);
        detail!.History.Select(h => h.Type).ShouldBe([FindingEventType.Reopened, FindingEventType.Resolved, FindingEventType.Opened]);
        detail.Occurrences.TotalCount.ShouldBe(12);

        // Taramalar listesinde değişim sayıları.
        var crawls = (await s.Client.GetFromJsonAsync<PagedResponse<CrawlListItemResponse>>($"/api/sites/{s.SiteId}/crawls", IntegrationTestContext.Json))!.Items;
        crawls.Single(c => c.Id == first).NewFindings.ShouldBeGreaterThan(0);
        crawls.Single(c => c.Id == second).ResolvedFindings.ShouldBeGreaterThanOrEqualTo(1);
        crawls.Single(c => c.Id == third).ReopenedFindings.ShouldBe(1);
        crawls.ShouldAllBe(c => c.EvaluatedAt != null);

        // Aynı tarama ikinci kez değerlendirilmez (iş tekrar çalışsa da kopya oluşmaz).
        await using (var scope = s.Factory.Services.CreateAsyncScope())
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new EvaluateCrawlCommand(third));
        (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem().Status.ShouldBe(FindingStatus.Reopened);
    }

    [Fact]
    public async Task False_positive_mutes_the_rule_for_the_page_type_and_survives_crawls()
    {
        var s = await ArrangeAsync();
        s.Site.SetProductTitles("Aynı ürün başlığı");
        await s.CrawlAndEvaluateAsync();
        var finding = (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem();

        (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/findings/{finding.Id}/false-positive", new { reason = "" }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/findings/{finding.Id}/false-positive", new { reason = "Ürün varyantları bilinçli olarak aynı adı taşıyor." }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);

        s.Site.SetProductTitles(null);
        await s.CrawlAndEvaluateAsync();
        (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem().Status.ShouldBe(FindingStatus.FalsePositive);

        var rule = await s.Client.GetFromJsonAsync<RuleDetailResponse>($"/api/sites/{s.SiteId}/rules/{TitleDuplicate}", IntegrationTestContext.Json);
        rule!.Muted.ShouldHaveSingleItem().PageTypeName.ShouldBe("Ürün");
        rule.PageTypes.Single(p => p.Name == "Ürün").IsMuted.ShouldBeTrue();
    }

    [Fact]
    public async Task Rule_settings_are_validated_and_threshold_impact_is_previewed()
    {
        var s = await ArrangeAsync();
        await s.CrawlAndEvaluateAsync();

        (await s.Client.PutAsJsonAsync($"/api/sites/{s.SiteId}/rules/content.thin", new { isEnabled = true, thresholds = new Dictionary<string, decimal> { ["minWords"] = 5 } }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Sahte sayfalar birkaç kelimelik; eşik 50'de hepsi zayıf içerik. Eşik 2'ye inerse hiçbiri kalmaz.
        var impact = await (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/rules/content.thin/impact",
            new { thresholds = new Dictionary<string, decimal> { ["minWords"] = 50 } })).Content.ReadFromJsonAsync<ThresholdImpactResponse>(IntegrationTestContext.Json);
        impact!.CrawlNumber.ShouldBe(1);
        impact.CurrentPages.ShouldBeGreaterThan(0);
        impact.NewPages.ShouldBe(impact.CurrentPages);

        (await s.Client.PutAsJsonAsync($"/api/sites/{s.SiteId}/rules/content.thin", new { isEnabled = false, thresholds = new Dictionary<string, decimal> { ["minWords"] = 100 } }))
            .StatusCode.ShouldBe(HttpStatusCode.OK);
        var rule = await s.Client.GetFromJsonAsync<RuleDetailResponse>($"/api/sites/{s.SiteId}/rules/content.thin", IntegrationTestContext.Json);
        rule!.Rule.IsEnabled.ShouldBeFalse();
        rule.Thresholds.ShouldHaveSingleItem().Value.ShouldBe(100);

        // Kapalı kural sonraki taramada bulgularını çözmez.
        var before = (await s.FindingsAsync("content.thin")).Select(f => (f.Id, f.Status)).ToList();
        await s.CrawlAndEvaluateAsync();
        (await s.FindingsAsync("content.thin")).Select(f => (f.Id, f.Status)).ShouldBe(before, ignoreOrder: true);
    }

    [Fact]
    public async Task Screens_list_findings_details_rules_and_crawl_changes()
    {
        var s = await ArrangeAsync();
        s.Site.SetProductTitles("Aynı ürün başlığı");
        var crawlId = await s.CrawlAndEvaluateAsync();
        var finding = (await s.FindingsAsync(TitleDuplicate)).ShouldHaveSingleItem();

        var list = await s.Client.GetStringAsync($"/sites/{s.SiteId}/findings");
        list.ShouldContain("Aynı kural ve sayfa tipinden gelen bulgular tek satırda gruplanır.");
        list.ShouldContain(finding.Title);
        list.ShouldContain($"/sites/{s.SiteId}/findings/{finding.Id}");

        var filtered = new HttpRequestMessage(HttpMethod.Get, $"/sites/{s.SiteId}/findings?rule={TitleDuplicate}&status=any");
        filtered.Headers.Add("HX-Request", "true");
        filtered.Headers.Add("HX-Target", "findings-results");
        var partial = await (await s.Client.SendAsync(filtered)).Content.ReadAsStringAsync();
        partial.ShouldStartWith("<div id=\"findings-results\"");
        partial.ShouldContain(finding.Title);
        partial.ShouldContain("Kural:");                     // aktif filtre chip'i

        var csv = await s.Client.GetAsync($"/sites/{s.SiteId}/findings/export?rule={TitleDuplicate}");
        csv.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        (await csv.Content.ReadAsStringAsync()).ShouldContain(finding.Title);

        var detail = await s.Client.GetStringAsync($"/sites/{s.SiteId}/findings/{finding.Id}");
        detail.ShouldContain("Problem");
        detail.ShouldContain("Ürün sayfalarından 12 tanesi etkileniyor.");
        detail.ShouldContain($"https://{s.Site.Domain}/urun/p1");
        detail.ShouldContain("Google Search Central");
        detail.ShouldNotContain("Düzelt");

        var rules = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/rules");
        rules.ShouldContain("Teknik SEO");
        rules.ShouldContain(TitleDuplicate);

        var rule = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/rules/seo.title.length");
        rule.ShouldContain("En kısa title");
        rule.ShouldContain("Sayfa tipine göre önem");

        var crawl = await s.Client.GetStringAsync($"/sites/{s.SiteId}/crawls/{crawlId}");
        crawl.ShouldContain("Bulgu değişimleri");
        crawl.ShouldContain(finding.Title);
    }
}
