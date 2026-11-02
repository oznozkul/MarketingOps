using System.Net;
using System.Text.RegularExpressions;
using MarketingOps.Application.Notifications.Queries.GetNotificationSettings;
using MarketingOps.Domain.Notifications;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Web;

[Collection(IntegrationTestCollection.Name)]
public partial class NotificationScreenTests(IntegrationTestContext context)
{
    private readonly HttpClient _client = context.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Rule_form_shows_domain_errors_and_saves_normalized_recipients()
    {
        var (siteId, settingsUrl) = await ArrangeAsync();
        var page = await _client.GetStringAsync(settingsUrl);
        page.ShouldContain("Bildirim kuralları");
        page.ShouldContain("Günlük özet");

        var invalid = await PostAsync(page, $"{settingsUrl}/rules/NewCriticalFinding",
            ("Enabled", "true"), ("Channels", "Email"), ("RepeatIntervalHours", "24"));
        (await BodyAsync(invalid)).ShouldContain("E-posta kanalı için en az bir alıcı gerekli.");

        var saved = await PostAsync(page, $"{settingsUrl}/rules/NewCriticalFinding",
            ("Enabled", "true"), ("Channels", "InApp"), ("Channels", "Email"), ("Recipients", "Ekip@Ornek.com"), ("QuietStart", "22:00"), ("QuietEnd", "07:00"), ("RepeatIntervalHours", "12"));
        Trigger(saved).ShouldContain("kuralı kaydedildi");
        (await BodyAsync(saved)).ShouldContain("ekip@ornek.com");

        var rule = (await SettingsAsync(siteId)).Rules.Single(r => r.EventType == NotificationEventType.NewCriticalFinding);
        rule.Enabled.ShouldBeTrue();
        rule.Channels.ShouldBe([NotificationChannelKind.InApp, NotificationChannelKind.Email], ignoreOrder: true);
        rule.QuietStart.ShouldBe(new TimeOnly(22, 0));
        rule.RepeatIntervalHours.ShouldBe(12);
    }

    [Fact]
    public async Task In_app_test_notification_appears_in_the_bell_and_opening_it_marks_it_read()
    {
        var (siteId, settingsUrl) = await ArrangeAsync();
        var page = await _client.GetStringAsync(settingsUrl);

        var test = await PostAsync(page, $"{settingsUrl}/test/InApp");
        test.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        Trigger(test).ShouldContain("Test bildirimi panele yazıldı.");

        var panel = await _client.GetStringAsync($"/sites/{siteId}/notifications");
        panel.ShouldContain("MarketingOps test bildirimi");
        panel.ShouldContain("1 okunmamış");

        var open = await _client.GetAsync(OpenLink().Match(panel).Value);
        open.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        open.Headers.Location!.ToString().ShouldBe($"/sites/{siteId}/overview");
        (await _client.GetStringAsync($"/sites/{siteId}/notifications")).ShouldNotContain("okunmamış");
    }

    [Fact]
    public async Task Test_on_a_channel_without_webhook_shows_an_error_toast()
    {
        var (_, settingsUrl) = await ArrangeAsync();
        var page = await _client.GetStringAsync(settingsUrl);

        var test = await PostAsync(page, $"{settingsUrl}/test/Teams");

        var trigger = Trigger(test);
        trigger.ShouldContain("Webhook adresi yok.");
        trigger.ShouldContain("error");
    }

    [Fact]
    public async Task Report_preview_is_the_summary_email()
    {
        var (_, settingsUrl) = await ArrangeAsync();

        var preview = await _client.GetAsync($"{settingsUrl}/reports/Weekly/preview");

        preview.Content.Headers.ContentType!.MediaType.ShouldBe("text/html");
        (await preview.Content.ReadAsStringAsync()).ShouldContain("Sahte mağaza");
    }

    private async Task<(Guid SiteId, string SettingsUrl)> ArrangeAsync()
    {
        var siteId = await TestSites.CreateAsync(_client, UniqueDomain.Next());
        return (siteId, $"/sites/{siteId}/settings/notifications");
    }

    private Task<HttpResponseMessage> PostAsync(string page, string url, params (string Key, string Value)[] fields) =>
        HtmxForm.PostAsync(_client, page, url, fields);

    private static Task<string> BodyAsync(HttpResponseMessage response) => HtmxForm.BodyAsync(response);

    private static string Trigger(HttpResponseMessage response) => HtmxForm.Trigger(response);

    private async Task<NotificationSettingsResponse> SettingsAsync(Guid siteId)
    {
        await using var scope = context.Factory.Services.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<ISender>().Send(new GetNotificationSettingsQuery(siteId));
    }

    [GeneratedRegex("""/sites/[0-9a-f-]{36}/notifications/[0-9a-f-]{36}""")]
    private static partial Regex OpenLink();
}
