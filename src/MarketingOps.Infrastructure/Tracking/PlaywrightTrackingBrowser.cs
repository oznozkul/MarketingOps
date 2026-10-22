using MarketingOps.Application.Tracking;
using Microsoft.Playwright;

namespace MarketingOps.Infrastructure.Tracking;

/// <summary>Her temsilci adresi bir kez açar. Onay seçicisi yoksa ya da tıklama olmazsa onay öncesi işaretlenmez.</summary>
public sealed class PlaywrightTrackingBrowser : ITrackingBrowser
{
    private const int NavigationTimeoutMs = 20_000;
    private const int SettleMs = 1_200;
    private const int ClickTimeoutMs = 3_000;

    public async Task<IReadOnlyList<PageObservation>> ProbeAsync(IReadOnlyList<string> urls, string? consentSelector, CancellationToken cancellationToken)
    {
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        await using var context = await browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });
        var results = new List<PageObservation>();
        foreach (var url in urls)
        {
            cancellationToken.ThrowIfCancellationRequested();
            results.Add(await ProbePage(context, url, consentSelector));
        }
        return results;
    }

    private static async Task<PageObservation> ProbePage(IBrowserContext context, string url, string? consentSelector)
    {
        var page = await context.NewPageAsync();
        var before = true;
        var captured = new List<ObservedRequest>();
        void OnRequest(object? _, IRequest request)
        {
            if (request.Url.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                captured.Add(new ObservedRequest(request.Url, before));
        }

        page.Request += OnRequest;
        try
        {
            await page.GotoAsync(url, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = NavigationTimeoutMs });
            await page.WaitForTimeoutAsync(SettleMs);
            var clicked = false;
            if (!string.IsNullOrWhiteSpace(consentSelector))
            {
                try
                {
                    await page.Locator(consentSelector).ClickAsync(new LocatorClickOptions { Timeout = ClickTimeoutMs });
                    clicked = true;
                    before = false;
                    await page.WaitForTimeoutAsync(SettleMs);
                }
                catch (TimeoutException)
                {
                    clicked = false;
                }
            }

            // Tıklama yoksa "onay öncesi" iddiası kurulamaz; istekler tagsız senaryo olarak kalır.
            var requests = clicked ? captured : captured.Select(r => r with { BeforeConsent = false }).ToList();
            return new PageObservation(url, true, clicked, requests);
        }
        catch (TimeoutException)
        {
            return new PageObservation(url, false, false, []);
        }
        catch (PlaywrightException)
        {
            return new PageObservation(url, false, false, []);
        }
        finally
        {
            page.Request -= OnRequest;
            await page.CloseAsync();
        }
    }
}
