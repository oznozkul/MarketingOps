using MarketingOps.Application.Rendering;
using MarketingOps.Domain.Rendering;
using MarketingOps.Infrastructure.Rendering;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketingOps.IntegrationTests.Rendering;

/// <summary>
/// render-audit.js gerçek Chromium'da: bilinen sorunları taşıyan yerel bir sayfa her cihazda ölçülür. Chromium kurulu olmalı
/// (pwsh src/MarketingOps.Worker/bin/Debug/net10.0/playwright.ps1 install chromium); kurulu değilse test başarısız olur.
/// </summary>
public class RenderAuditScriptTests
{
    // 1×1 PNG: doğal oranı 1; 300×100 çizilince %200 bozulur.
    private const string Pixel = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

    private static readonly string Page = $$"""
        <!doctype html>
        <html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
        <style>
          body { margin: 0; font: 16px/1.4 sans-serif; color: #111; background: #fff; }
          .genis { width: 600px; height: 20px; background: #eee; }
          .soluk { color: #bbb; }
          .kucuk { font-size: 10px; }
          .ikon { display: inline-block; width: 20px; height: 20px; }
          .slider { width: 300px; overflow: hidden; }
          .iz { display: flex; width: 600px; }
          .iz img { width: 300px; height: 100px; flex: none; object-fit: cover; }
          .uzak { margin-top: 3000px; }
        </style></head>
        <body>
          <img class="banner" src="{{Pixel}}" width="300" height="100" alt="Kampanya" loading="lazy">
          <div class="slider"><div class="iz">
            <img class="slayt1" src="{{Pixel}}" alt="1" loading="lazy"><img class="slayt2" src="{{Pixel}}" alt="2" loading="lazy">
          </div></div>
          <p class="soluk">Soluk fiyat metni</p>
          <p><span class="kucuk">Küçük yasal not</span></p>
          <p><a class="ikon" href="/favori">x</a> metin içi <a href="/kargo">kargo bilgisi</a></p>
          <div class="genis">Geniş kutu</div>
          <img class="uzak" src="{{Pixel}}" width="100" height="100" alt="Ekran altı">
        </body></html>
        """;

    private static async Task<RenderResult> Measure(string html, RenderDevice device)
    {
        var file = Path.Combine(Path.GetTempPath(), $"render-{Guid.NewGuid():N}.html");
        await File.WriteAllTextAsync(file, html);
        try
        {
            var auditor = new PlaywrightRenderAuditor(NullLogger<PlaywrightRenderAuditor>.Instance);
            var results = await auditor.AuditAsync([new RenderTarget(new Uri(file).AbsoluteUri, null, device)], new RenderOptions(10, null), CancellationToken.None);
            return results.ShouldHaveSingleItem();
        }
        finally { File.Delete(file); }
    }

    [Fact]
    public async Task Phone_measurement_finds_overflow_small_text_tap_targets_contrast_and_fold_issues()
    {
        var result = await Measure(Page, RenderDevice.Mobile);

        result.Error.ShouldBeNull();
        result.Obstructed.ShouldBeFalse();
        var m = result.Measurements!;
        m.ViewportWidth.ShouldBe(375);
        m.DocumentWidth.ShouldBeGreaterThan(379);
        m.OverflowOffenders.ShouldContain(e => e.Selector.Contains("div.genis"));
        m.SmallText.ShouldContain(e => e.Selector.Contains("span.kucuk") && e.Value == 10);
        m.SmallTapTargets.ShouldContain(e => e.Selector.Contains("a.ikon") && e.Value == 20);
        m.SmallTapTargets.ShouldNotContain(e => e.Text == "kargo bilgisi");            // metin içi link sayılmaz
        m.ContrastFailures.ShouldContain(e => e.Selector.Contains("p.soluk") && e.Limit == 4.5 && e.Value < 4.5);

        // Karuselde yalnızca görünen slayt ilk ekrandadır; ekran altındaki loading'siz görsel hemen yüklenir.
        m.LazyAboveFold.ShouldContain(e => e.Selector.Contains("img.banner"));
        m.LazyAboveFold.ShouldContain(e => e.Selector.Contains("img.slayt1"));
        m.LazyAboveFold.ShouldNotContain(e => e.Selector.Contains("img.slayt2"));
        m.EagerBelowFold.ShouldContain(e => e.Selector.Contains("img.uzak"));
        m.DistortedImages.ShouldContain(e => e.Selector.Contains("img.banner"));
        m.DistortedImages.ShouldNotContain(e => e.Selector.Contains("img.slayt1"));   // object-fit: cover bozmaz
    }

    [Fact]
    public async Task Desktop_measurement_has_no_overflow_and_skips_tap_targets()
    {
        var m = (await Measure(Page, RenderDevice.Desktop)).Measurements!;

        m.ViewportWidth.ShouldBe(1366);
        m.DocumentWidth.ShouldBeLessThanOrEqualTo(1370);
        m.SmallTapTargets.ShouldBeEmpty();
        m.WideElements.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sites_with_a_theme_attribute_are_measured_in_both_themes()
    {
        var html = """
            <!doctype html><html data-theme="light"><head><style>
              body { background: #fff; color: #111; }
              [data-theme="dark"] body { background: #1a1a1a; color: #eee; }
              [data-theme="dark"] .gece { color: #333; }
              @media (prefers-color-scheme: dark) { .her { font-style: normal; } }
            </style></head><body><p class="gece">Yalnızca koyu temada okunmaz</p><p class="her">Her temada okunur</p></body></html>
            """;

        var m = (await Measure(html, RenderDevice.Desktop)).Measurements!;

        // Sitede ayrıca prefers-color-scheme olsa da koyu tema ikinci kez ölçülmez (tek satır).
        var failure = m.ContrastFailures.ShouldHaveSingleItem();
        failure.Selector.ShouldContain("p.gece");
        failure.Text!.ShouldStartWith("koyu tema:");
    }

    [Fact]
    public async Task Sites_with_a_dark_color_scheme_media_query_are_measured_in_dark_mode()
    {
        var html = """
            <!doctype html><html><head><style>
              body { background: #fff; color: #111; }
              @media (prefers-color-scheme: dark) { body { background: #1a1a1a; color: #eee; } .gece { color: #333; } }
            </style></head><body><p class="gece">Yalnızca koyu modda okunmaz</p><p>Her modda okunur</p></body></html>
            """;

        var m = (await Measure(html, RenderDevice.Desktop)).Measurements!;

        var failure = m.ContrastFailures.ShouldHaveSingleItem();
        failure.Selector.ShouldContain("p.gece");
        failure.Text!.ShouldStartWith("koyu tema:");
    }

    [Fact]
    public async Task Evidence_screenshot_covers_the_page_below_the_fold()
    {
        var result = await Measure(Page.Replace("</body>", """<div style="height:2000px"></div></body>"""), RenderDevice.Mobile);

        result.ScreenshotJpeg.ShouldNotBeNull();
        result.ScreenshotHeight.ShouldBeGreaterThan(812);
        JpegHeight(result.ScreenshotJpeg).ShouldBe(result.ScreenshotHeight);
    }

    [Fact]
    public async Task An_image_link_with_empty_alt_counts_as_a_visible_link_without_name()
    {
        var html = $$"""<!doctype html><html><body><a href="/sepet"><img src="{{Pixel}}" width="40" height="40" alt=""></a><a href="/x"><img src="{{Pixel}}" width="40" height="40" alt="Sepet"></a></body></html>""";

        var m = (await Measure(html, RenderDevice.Desktop)).Measurements!;

        m.HasVisibilityCounts.ShouldBeTrue();
        m.VisibleNoTextLinks.ShouldBe(1);
    }

    /// <summary>JPEG'in SOF bölümündeki görüntü yüksekliği (piksel).</summary>
    private static int JpegHeight(byte[] jpeg)
    {
        for (var i = 2; i + 8 < jpeg.Length;)
        {
            if (jpeg[i] != 0xFF) { i++; continue; }
            var marker = jpeg[i + 1];
            if (marker is >= 0xC0 and <= 0xC3) return (jpeg[i + 5] << 8) | jpeg[i + 6];
            i += 2 + ((jpeg[i + 2] << 8) | jpeg[i + 3]);
        }
        throw new InvalidOperationException("JPEG yüksekliği okunamadı.");
    }

    [Fact]
    public async Task A_fixed_layer_covering_the_screen_marks_the_measurement_obstructed()
    {
        var html = Page.Replace("</body>", """<div style="position:fixed;inset:0;background:#000c">Çerez onayı</div></body>""");

        (await Measure(html, RenderDevice.Desktop)).Obstructed.ShouldBeTrue();
    }
}
