using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors.Commands.ConnectMeta;
using MarketingOps.Application.Sites.Commands.UpdateExpectedTrackingIds;
using MarketingOps.Application.Sites.Commands.UpdateOrderSource;
using MarketingOps.Application.Tracking;
using MarketingOps.Application.Tracking.Commands.RunTrackingAudit;
using MarketingOps.Domain.Findings;
using MarketingOps.Domain.Metrics;
using MarketingOps.Domain.Rules.Definitions;
using MarketingOps.Infrastructure.Persistence;
using MarketingOps.IntegrationTests.Crawling;
using MarketingOps.IntegrationTests.Infrastructure;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MarketingOps.IntegrationTests.Tracking;

[Collection(IntegrationTestCollection.Name)]
public class TrackingAuditTests(IntegrationTestContext context)
{
    [Fact]
    public async Task Audit_opens_tag_and_gap_findings_and_does_not_resolve_them_without_ga4_or_meta()
    {
        var browser = new ScriptedBrowser();
        var factory = context.Factory.WithWebHostBuilder(b => b.ConfigureTestServices(s =>
        {
            s.AddSingleton<ITrackingBrowser>(browser);
            s.AddSingleton<IOrderSource>(new SteadyOrders());
            s.AddSingleton<IMetaGraphClient>(new FixedMeta(new MetaSignals(true, false, false)));
        }));
        var client = factory.CreateClient();
        var siteId = await TestSites.CreateAsync(client, UniqueDomain.Next());

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new UpdateExpectedTrackingIdsCommand(siteId, "G-CORRECT", null, "123456789012345", null));
            await sender.Send(new UpdateOrderSourceCommand(siteId, "Server=unused;Database=unused;", "SELECT 1 AS order_count, 1 AS order_revenue WHERE day = @date"));
            await sender.Send(new ConnectMetaCommand(siteId, "test-token", false));
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Ga4Events.Add(Ga4EventDaily.Create(siteId, DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1)), "purchase", 50, 500));
            await db.SaveChangesAsync();
            await sender.Send(new RunTrackingAuditCommand(siteId));
        }

        var first = await Findings(factory, siteId);
        first["track.tag.wrong_id"].ShouldBe(FindingStatus.Open);
        first["track.consent.prefire"].ShouldBe(FindingStatus.Open);
        first["track.purchase.gap"].ShouldBe(FindingStatus.Open);
        first["track.meta.pixel_capi"].ShouldBe(FindingStatus.Open);
        first["track.meta.dedup"].ShouldBe(FindingStatus.Open);

        browser.Clean = true;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Ga4Events.RemoveRange(await db.Ga4Events.Where(e => e.SiteId == siteId).ToListAsync());
            await db.SaveChangesAsync();
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new ConnectMetaCommand(siteId, null, true));
            await scope.ServiceProvider.GetRequiredService<ISender>().Send(new RunTrackingAuditCommand(siteId));
        }

        var second = await Findings(factory, siteId);
        second["track.tag.wrong_id"].ShouldBe(FindingStatus.Resolved);
        second["track.consent.prefire"].ShouldBe(FindingStatus.Resolved);
        second["track.purchase.gap"].ShouldBe(FindingStatus.Open);
        second["track.meta.pixel_capi"].ShouldBe(FindingStatus.Open);
        second["track.meta.dedup"].ShouldBe(FindingStatus.Open);
    }

    private static async Task<Dictionary<string, FindingStatus>> Findings(WebApplicationFactory<Program> factory, Guid siteId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
        return await db.Findings.Where(f => f.SiteId == siteId).ToDictionaryAsync(f => f.RuleCode, f => f.Status);
    }

    private sealed class ScriptedBrowser : ITrackingBrowser
    {
        public bool Clean { get; set; }

        public Task<IReadOnlyList<PageObservation>> ProbeAsync(IReadOnlyList<string> urls, string? consentSelector, CancellationToken cancellationToken)
        {
            IReadOnlyList<PageObservation> pages = urls.Select(url => Clean
                ? new PageObservation(url, true, false, [new ObservedRequest("https://www.google-analytics.com/g/collect?tid=G-CORRECT", false)])
                : new PageObservation(url, true, true,
                [
                    new ObservedRequest("https://www.google-analytics.com/g/collect?tid=G-OTHER", false),
                    new ObservedRequest("https://www.facebook.com/tr?id=999", true)
                ])).ToList();
            return Task.FromResult(pages);
        }
    }

    private sealed class SteadyOrders : IOrderSource
    {
        public Task<IReadOnlyList<OrderDayStats>> GetDailyOrderStatsAsync(string connectionString, string query, DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            var days = new List<OrderDayStats>();
            for (var date = from; date <= to; date = date.AddDays(1)) days.Add(new OrderDayStats(date, 100, 1000));
            return Task.FromResult<IReadOnlyList<OrderDayStats>>(days);
        }
    }

    private sealed class FixedMeta(MetaSignals signals) : IMetaGraphClient
    {
        public Task<MetaSignals> ReadAsync(string accessToken, string pixelId, CancellationToken cancellationToken) => Task.FromResult(signals);
    }
}
