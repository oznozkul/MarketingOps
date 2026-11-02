using System.Text.Json;
using MarketingOps.Application.Rendering;
using MarketingOps.Domain.Rendering;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace MarketingOps.Infrastructure.Rendering;

/// <summary>
/// Örnek sayfaları Chromium'da her cihaz için ayrı bağlamla (gerçek viewport, dokunmatik, piksel yoğunluğu) açar ve
/// render-audit.js ile ölçer. Sayfalar tek tek, sitenin hız limitine uyularak açılır; bir sayfanın hatası diğerlerini durdurmaz.
/// </summary>
public sealed class PlaywrightRenderAuditor(ILogger<PlaywrightRenderAuditor> logger) : IRenderAuditor
{
    private const int NavigationTimeoutMs = 30_000;
    private const int NetworkIdleTimeoutMs = 5_000;
    private const int FontsTimeoutMs = 3_000;
    private const int SettleMs = 300;
    private const int ConsentTimeoutMs = 3_000;
    private const string Bot = "MarketingOpsBot/1.0 (render)";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private static readonly Lazy<string> Script = new(LoadScript);

    private static readonly Dictionary<RenderDevice, BrowserNewContextOptions> Devices = new()
    {
        [RenderDevice.Desktop] = new()
        {
            ViewportSize = new() { Width = 1366, Height = 900 },
            IgnoreHTTPSErrors = true,
            UserAgent = $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Safari/537.36 {Bot}"
        },
        [RenderDevice.Tablet] = new()
        {
            ViewportSize = new() { Width = 768, Height = 1024 },
            IgnoreHTTPSErrors = true,
            HasTouch = true,
            DeviceScaleFactor = 2,
            UserAgent = $"Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15 {Bot}"
        },
        [RenderDevice.Mobile] = new()
        {
            ViewportSize = new() { Width = 375, Height = 812 },
            IgnoreHTTPSErrors = true,
            IsMobile = true,
            HasTouch = true,
            DeviceScaleFactor = 2,
            UserAgent = $"Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0 Mobile Safari/537.36 {Bot}"
        }
    };

    public async Task<IReadOnlyList<RenderResult>> AuditAsync(IReadOnlyList<RenderTarget> targets, RenderOptions options, CancellationToken cancellationToken)
    {
        if (targets.Count == 0) return [];
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
        var delay = TimeSpan.FromSeconds(1.0 / Math.Max(1, options.MaxRequestsPerSecond));
        var results = new List<RenderResult>(targets.Count);

        foreach (var device in targets.GroupBy(t => t.Device))
        {
            await using var context = await browser.NewContextAsync(Devices[device.Key]);
            foreach (var target in device)
            {
                cancellationToken.ThrowIfCancellationRequested();
                results.Add(await MeasureAsync(context, target, options.ConsentSelector));
                await Task.Delay(delay, cancellationToken);
            }
        }
        return results;
    }

    private async Task<RenderResult> MeasureAsync(IBrowserContext context, RenderTarget target, string? consentSelector)
    {
        var page = await context.NewPageAsync();
        try
        {
            await page.GotoAsync(target.Url, new PageGotoOptions { WaitUntil = WaitUntilState.Load, Timeout = NavigationTimeoutMs });
            await QuietlyAsync(() => page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = NetworkIdleTimeoutMs }));
            if (!string.IsNullOrWhiteSpace(consentSelector))
                await QuietlyAsync(() => page.Locator(consentSelector).First.ClickAsync(new LocatorClickOptions { Timeout = ConsentTimeoutMs }));
            await QuietlyAsync(() => page.EvaluateAsync($"Promise.race([document.fonts.ready, new Promise(r => setTimeout(r, {FontsTimeoutMs}))])"));
            await page.WaitForTimeoutAsync(SettleMs);

            var output = await page.EvaluateAsync<JsonElement>(Script.Value, new { device = target.Device.ToString(), maxElements = RenderMeasurements.MaxElements });
            var measurements = output.GetProperty("measurements").Deserialize<RenderMeasurements>(Json) ?? RenderMeasurements.Empty;
            var obstructed = output.GetProperty("obstructed").GetBoolean();

            // Okunabilir stillerde @media (prefers-color-scheme: dark) varsa sayfa koyu renk şemasıyla yeniden ölçülür;
            // yalnızca kontrast sonuçları "koyu tema:" önekiyle birleştirilir (site data-theme kullanmıyorsa bu tek yoldur).
            var darkMediaQuery = output.TryGetProperty("darkMediaQuery", out var dm) && dm.GetBoolean();
            if (darkMediaQuery)
            {
                await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Dark });
                var darkOutput = await page.EvaluateAsync<JsonElement>(Script.Value, new { device = target.Device.ToString(), maxElements = RenderMeasurements.MaxElements, dark = true });
                var darkMeasurements = darkOutput.GetProperty("measurements").Deserialize<RenderMeasurements>(Json) ?? RenderMeasurements.Empty;
                measurements = measurements with
                {
                    ContrastFailures = measurements.ContrastFailures.Concat(darkMeasurements.ContrastFailures)
                        .DistinctBy(e => e.Selector).Take(RenderMeasurements.MaxElements).ToList()
                };
                // Kanıt görüntüsü ziyaretçinin varsayılan (açık) görünümünde alınır.
                await page.EmulateMediaAsync(new PageEmulateMediaOptions { ColorScheme = ColorScheme.Light });
            }

            var screenshot = await CaptureScreenshotAsync(page, measurements);
            return new RenderResult(target, measurements, obstructed, null, screenshot.Jpeg, screenshot.Height);
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException or JsonException or KeyNotFoundException)
        {
            logger.LogInformation("Render ölçümü alınamadı: {Url} ({Device}): {Error}", target.Url, target.Device, ex.Message);
            return new RenderResult(target, null, false, Reason(ex));
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    /// <summary>Ölçümde sorunlu öğe varsa kanıt ekran görüntüsü: sorunlular çerçevelenir, yükseklik 3000 px'i aşmaz. En iyi çabadır.</summary>
    private async Task<(byte[]? Jpeg, int Height)> CaptureScreenshotAsync(IPage page, RenderMeasurements measurements)
    {
        var selectors = Offenders(measurements);
        if (selectors.Count == 0) return (null, 0);
        try
        {
            // Her seçici ayrı kural: tarayıcı geçersiz seçicili kuralı tümden atar, biri bozuksa diğerleri yine çerçevelenir.
            var outline = string.Concat(selectors.Select(s => s + "{outline:3px solid #e11d48 !important;outline-offset:1px}\n"));
            // Bazı mobil düzenlerde html kaydırmayı kapatıp içeriği body içinde kaydırır; belge yüksekliği ekran kadar
            // görünür ve tam sayfa görüntü yalnızca ilk ekranı alır. Kısıt yalnızca bu tarayıcı kopyasında kaldırılır.
            const string unclamp = "html,body{height:auto !important;max-height:none !important;overflow:visible !important}\n";
            await page.AddStyleTagAsync(new PageAddStyleTagOptions { Content = unclamp + outline });
            var scrollHeight = await page.EvaluateAsync<int>("Math.max(document.documentElement.scrollHeight, document.body ? document.body.scrollHeight : 0)");
            var height = Math.Clamp(scrollHeight, 1, RenderScreenshot.MaxHeight);
            var width = Math.Max(1, measurements.ViewportWidth);
            var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Type = ScreenshotType.Jpeg,
                Quality = 70,
                // FullPage olmadan Playwright kırpmayı görünen alana indirir; ekranın altındaki öğeler görüntüye girmez.
                FullPage = true,
                Scale = ScreenshotScale.Css, // telefonun piksel yoğunluğu görüntüyü 3 katına çıkarmasın; yükseklik CSS pikselidir

                Clip = new Clip { X = 0, Y = 0, Width = width, Height = height }
            });
            return (bytes, height);
        }
        catch (Exception ex) when (ex is PlaywrightException or TimeoutException)
        {
            logger.LogInformation("Ekran görüntüsü alınamadı: {Error}", ex.Message);
            return (null, 0);
        }
    }

    private static IReadOnlyList<string> Offenders(RenderMeasurements m) =>
        m.OverflowOffenders.Concat(m.WideElements).Concat(m.ContrastFailures).Concat(m.SmallText)
            .Concat(m.SmallTapTargets).Concat(m.LazyAboveFold).Concat(m.EagerBelowFold).Concat(m.DistortedImages)
            .Select(e => e.Selector)
            .Where(s => !string.IsNullOrWhiteSpace(s) && !s.EndsWith('…')) // ölçüm betiğinin kırptığı seçici geçersiz CSS'tir
            .Distinct(StringComparer.Ordinal).ToList();

    /// <summary>Bekleme adımları en iyi çabadır: ağ hiç sakinleşmeyen ya da onay düğmesi olmayan sayfa yine ölçülür.</summary>
    private static async Task QuietlyAsync(Func<Task> step)
    {
        try { await step(); }
        catch (Exception ex) when (ex is TimeoutException or PlaywrightException) { }
    }

    private static string Reason(Exception ex) => ex switch
    {
        TimeoutException => "Sayfa zaman aşımına uğradı.",
        JsonException or KeyNotFoundException => "Ölçüm sonucu okunamadı.",
        _ => ex.Message.Split('\n')[0]
    };

    private static string LoadScript()
    {
        using var stream = typeof(PlaywrightRenderAuditor).Assembly.GetManifestResourceStream("MarketingOps.Infrastructure.Rendering.render-audit.js")
            ?? throw new InvalidOperationException("render-audit.js gömülü kaynak olarak bulunamadı.");
        using var reader = new StreamReader(stream);
        var text = reader.ReadToEnd();
        // Baştaki açıklama satırları atılır: Playwright metni ancak fonksiyonla başlıyorsa fonksiyon olarak çağırır.
        return text[text.IndexOf("(options) =>", StringComparison.Ordinal)..];
    }
}
