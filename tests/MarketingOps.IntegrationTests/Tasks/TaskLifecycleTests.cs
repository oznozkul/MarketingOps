using System.Net;
using System.Net.Http.Json;
using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Common.Models;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MarketingOps.Application.Tasks.Commands.EnrichTask;
using MarketingOps.Application.Tasks.Queries;
using MarketingOps.Application.Tasks.Queries.GetTaskById;
using MarketingOps.Domain.WorkTasks;
using MarketingOps.Infrastructure.Crawling;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Tasks;

[Collection(IntegrationTestCollection.Name)]
public class TaskLifecycleTests(IntegrationTestContext context)
{
    private sealed record Scenario(WebApplicationFactory<Program> Factory, HttpClient Client, SiteCrawler Crawler, FakeSite Site, Guid SiteId,
        FakeAiSuggestionService Ai)
    {
        public async Task CrawlAndEvaluateAsync()
        {
            var crawlId = await TestSites.StartCrawlAsync(Client, SiteId);
            await Crawler.RunAsync(crawlId, CancellationToken.None);
            await SendAsync(new EvaluateCrawlCommand(crawlId));
        }

        public async Task SendAsync(IRequest request)
        {
            await using var scope = Factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(request);
        }

        public async Task<TaskDetailResponse> TitleDuplicateTaskAsync()
        {
            var tasks = await Client.GetFromJsonAsync<PagedResponse<TaskListItemResponse>>($"/api/sites/{SiteId}/tasks?search=title&pageSize=100", IntegrationTestContext.Json);
            var item = tasks!.Items.Where(t => t.Title.Contains("title tekrar")).ShouldHaveSingleItem();
            return (await Client.GetFromJsonAsync<TaskDetailResponse>($"/api/sites/{SiteId}/tasks/{item.Id}", IntegrationTestContext.Json))!;
        }
    }

    private async Task<Scenario> ArrangeAsync(bool withAi = false)
    {
        var site = new FakeSite(UniqueDomain.Next());
        var ai = new FakeAiSuggestionService();
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<ICrawlerHttpClient>(site);
            if (withAi) s.AddSingleton<IAiSuggestionService>(ai);
        }));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, site.Domain);
        return new Scenario(factory, client, factory.Services.GetRequiredService<SiteCrawler>(), site, siteId, ai);
    }

    [Fact]
    public async Task Finding_opens_a_task_scan_meets_automatic_criteria_team_confirmation_closes_and_regression_reopens()
    {
        var s = await ArrangeAsync();
        s.Site.SetProductTitles("Aynı ürün başlığı");
        await s.CrawlAndEvaluateAsync();

        // Bulgu açıldı → görev otomatik oluştu (sitenin adından türetilen önekle), öneri şablondan.
        var task = await s.TitleDuplicateTaskAsync();
        task.Key.ShouldStartWith("SM-");
        task.Status.ShouldBe(WorkTaskStatus.Todo);
        task.Team.ShouldBe(Team.Seo);
        task.Problem.ShouldStartWith("Ürün sayfalarından 12 tanesi etkileniyor.");
        task.Suggestion.Model.ShouldBeNull();
        task.Suggestion.Examples.Count.ShouldBe(TaskTemplate.ExampleCount);
        task.Criteria.Select(c => c.VerificationType).ShouldBe([VerificationType.Automatic, VerificationType.TeamConfirmation]);
        task.AiModel.ShouldBeNull();

        // Otomatik kriter kullanıcı tarafından işaretlenemez.
        var automatic = task.Criteria.Single(c => c.VerificationType == VerificationType.Automatic);
        (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/tasks/{task.Id}/criteria/{automatic.Id}", new { confirmed = true }))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Sorun düzeldi → otomatik kriter sağlandı, ekip onayı bekleniyor.
        s.Site.SetProductTitles(null);
        await s.CrawlAndEvaluateAsync();
        task = await s.TitleDuplicateTaskAsync();
        task.Status.ShouldBe(WorkTaskStatus.AwaitingVerification);
        task.Criteria.Single(c => c.VerificationType == VerificationType.Automatic).IsMet.ShouldBeTrue();

        // Ekip kriteri onaylandı → görev kapandı.
        var team = task.Criteria.Single(c => c.VerificationType == VerificationType.TeamConfirmation);
        (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/tasks/{task.Id}/criteria/{team.Id}", new { confirmed = true }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        task = await s.TitleDuplicateTaskAsync();
        task.Status.ShouldBe(WorkTaskStatus.Closed);
        task.ClosedAt.ShouldNotBeNull();

        // Sorun geri geldi → aynı görev Yapılacak'a döner, yeni görev açılmaz.
        s.Site.SetProductTitles("Aynı ürün başlığı");
        await s.CrawlAndEvaluateAsync();
        var reopened = await s.TitleDuplicateTaskAsync();
        reopened.Id.ShouldBe(task.Id);
        reopened.Key.ShouldBe(task.Key);
        reopened.Status.ShouldBe(WorkTaskStatus.Todo);
        reopened.History.First().Type.ShouldBe(TaskEventType.Reopened);

        await using var scope = s.Factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        (await db.WorkTasks.CountAsync(t => t.SiteId == s.SiteId)).ShouldBe(await db.Findings.CountAsync(f => f.SiteId == s.SiteId));
    }

    [Fact]
    public async Task Ai_enrichment_changes_text_but_keeps_numbers_and_only_accepts_given_urls()
    {
        var s = await ArrangeAsync(withAi: true);
        s.Site.SetProductTitles("Aynı ürün başlığı");
        await s.CrawlAndEvaluateAsync();
        var before = await s.TitleDuplicateTaskAsync();
        before.AiModel.ShouldBe("sahte-model");

        await s.SendAsync(new EnrichTaskCommand(before.Id));

        var after = await s.TitleDuplicateTaskAsync();
        after.Suggestion.Model.ShouldBe("sahte-model");
        after.Impact.ShouldBe("AI etki metni.");
        after.Suggestion.Content.ShouldBe("AI içeriği");
        after.Problem.ShouldBe(before.Problem);
        after.Suggestion.Examples.Select(e => e.Url).ShouldBe(before.Suggestion.Examples.Select(e => e.Url));
        after.Suggestion.Examples[0].Suggested.ShouldBe("Önerilen başlık");
        after.Suggestion.Examples.Skip(1).ShouldAllBe(e => e.Suggested == null);
        after.History.First().Type.ShouldBe(TaskEventType.SuggestionGenerated);

        var request = s.Ai.Requests.Last();
        request.AffectedCount.ShouldBe(12);
        request.ReferenceKey.ShouldBe("google:title-links");

        (await s.Client.PostAsJsonAsync($"/api/sites/{s.SiteId}/tasks/{after.Id}/feedback", new { isHelpful = true, note = "iyi" }))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await s.TitleDuplicateTaskAsync()).HelpfulCount.ShouldBe(1);
    }

    [Fact]
    public async Task Screens_show_tasks_list_board_detail_and_settings()
    {
        var s = await ArrangeAsync();
        s.Site.SetProductTitles("Aynı ürün başlığı");
        await s.CrawlAndEvaluateAsync();
        var task = await s.TitleDuplicateTaskAsync();

        var list = await s.Client.GetStringAsync($"/sites/{s.SiteId}/tasks");
        list.ShouldContain(task.Key);
        list.ShouldNotContain("açık bulgunun henüz görevi yok");

        var board = await s.Client.GetStringAsync($"/sites/{s.SiteId}/tasks?view=board");
        board.ShouldContain("Yapılacak");
        board.ShouldContain(task.Key);

        var redirect = await s.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false })
            .GetAsync($"/sites/{s.SiteId}/tasks/{task.Id}");
        redirect.Headers.Location!.ToString().ShouldBe($"/sites/{s.SiteId}/findings/{task.FindingId}#gorev");

        var detail = await s.Client.GetStringAsync($"/sites/{s.SiteId}/findings/{task.FindingId}");
        detail.ShouldContain("Önerilen çözüm");
        detail.ShouldContain("Kural şablonundan · uygulamayı ekip yapar");
        detail.ShouldContain("Kabul kriterleri");
        detail.ShouldContain("Otomatik · sonraki tarama");
        detail.ShouldContain("Öneri faydalı mı?");
        detail.ShouldNotContain(">Düzelt<");

        var findings = await s.Client.GetStringAsync($"/sites/{s.SiteId}/findings");
        findings.ShouldContain(task.Key);
        findings.ShouldContain("Görev oluştur");

        var settings = await s.Client.GetStringAsync($"/sites/{s.SiteId}/settings/sites");
        settings.ShouldContain("AI önerileri");
        settings.ShouldContain("Görev öneki");
    }
}
