using MarketingOps.Domain.Findings;
using MarketingOps.Web.Infrastructure;
using MarketingOps.Web.ViewComponents;
using MarketingOps.Web.ViewModels;
using MarketingOps.Web.ViewModels.Design;
using Microsoft.AspNetCore.Mvc;

namespace MarketingOps.Web.Controllers;

/// <summary>
/// Tasarım sistemi vitrini. Yalnızca Development ortamında açıktır. Bileşenleri göstermek için örnek veri kullanır;
/// bu veri hiçbir ekranda ve veritabanında kullanılmaz.
/// </summary>
[Route("_design")]
public class DesignController(IWebHostEnvironment env, TimeProvider clock) : Controller
{
    private const string BreakdownUrl = "/_design/score-breakdown";

    [HttpGet("")]
    public IActionResult Index(string? sort, bool desc, int page = 1, int pageSize = 25)
    {
        if (!env.IsDevelopment()) return NotFound();
        ViewData["Title"] = "Tasarım sistemi";

        var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
        var rows = SortRows(SampleRows(clock.GetUtcNow()), sort ?? "severity", desc).ToList();
        pageSize = PaginationViewComponent.Sizes.Contains(pageSize) ? pageSize : PaginationViewComponent.Sizes[0];
        page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(rows.Count / (double)pageSize)));

        return View(new DesignViewModel(
            [
                new ScoreCardViewModel("Tracking Sağlığı", 43, 43, BreakdownUrl),
                new ScoreCardViewModel("SEO Sağlığı", 78, 77, BreakdownUrl),
                new ScoreCardViewModel("Performans", 66, 66, BreakdownUrl),
                new ScoreCardViewModel("Tracking Sağlığı", null, null, null, "GA4", "#")
            ],
            rows.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            page, pageSize, rows.Count, sort ?? "severity", desc,
            SampleOrders(today),
            SampleTrend(today),
            [
                new(today.AddDays(-12), "Deploy: katalog v3.8", "deploy"),
                new(today.AddDays(-3), "Deploy: ödeme sayfası v2.4", "deploy"),
                new(today.AddDays(-2), "Kritik bulgu: GA4 purchase sayısı siparişlerin altında", "finding")
            ]));
    }

    [HttpGet("score-breakdown")]
    public IActionResult ScoreBreakdown()
    {
        if (!env.IsDevelopment()) return NotFound();
        IReadOnlyList<DesignScoreDeduction> items =
        [
            new("GA4 purchase sayısı siparişlerin %17 altında", Severity.Critical, "Teşekkürler", 1, 1, 10),
            new("Meta Pixel çerez onayından önce tetikleniyor", Severity.Critical, "Ana sayfa", 1, 1, 10),
            new("Ürün sayfasında GA4 iki kez yükleniyor", Severity.High, "Ürün", 8420, 8420, 8),
            new("Ürün sayfalarında Meta Pixel ViewContent gönderilmiyor", Severity.High, "Ürün", 7310, 8420, 6.9m)
        ];
        return PartialView("_ScoreBreakdownSample", items);
    }

    /// <summary>Yükleniyor durumunu göstermek için yapay gecikmeli parça.</summary>
    [HttpGet("slow")]
    public async Task<IActionResult> Slow(CancellationToken ct)
    {
        if (!env.IsDevelopment()) return NotFound();
        await Task.Delay(1500, ct);
        return Content("<p class=\"text-sm text-fg\">Yüklendi. Bu içerik 1,5 saniye gecikmeyle geldi.</p>", "text/html");
    }

    /// <summary>Bölgesel hata durumunu gösterir: htmx bölgesi yalnızca kendi içinde hata gösterir.</summary>
    [HttpGet("fail")]
    public IActionResult Fail()
    {
        if (!env.IsDevelopment()) return NotFound();
        return PartialView("_ErrorState", "Örnek hata: GA4 API'si zaman aşımına uğradı.");
    }

    [HttpPost("toast")]
    [ValidateAntiForgeryToken]
    public IActionResult Toast(string kind = "ok")
    {
        if (!env.IsDevelopment()) return NotFound();
        Response.Toast(kind == "error" ? "İşlem tamamlanamadı. Tekrar deneyin." : "Değişiklik kaydedildi.", kind);
        return NoContent();
    }

    private static IEnumerable<DesignFindingRow> SortRows(IEnumerable<DesignFindingRow> rows, string sort, bool desc)
    {
        Func<DesignFindingRow, object> key = sort.ToLowerInvariant() switch
        {
            "title" => r => r.Title,
            "affectedpages" => r => r.AffectedPageCount,
            "lastseen" => r => r.LastSeenAt,
            _ => r => r.Severity
        };
        return desc ? rows.OrderByDescending(key) : rows.OrderBy(key);
    }

    private static List<DesignFindingRow> SampleRows(DateTimeOffset now)
    {
        (string Title, string Code, string PageType, Severity Severity, FindingStatus Status, int Pages)[] samples =
        [
            ("389 Lastik ölçü sayfasında noindex var", "seo.noindex.unexpected", "Lastik ölçü", Severity.Critical, FindingStatus.Open, 389),
            ("GA4 purchase sayısı siparişlerin %17 altında", "track.purchase.gap", "Teşekkürler", Severity.Critical, FindingStatus.Reopened, 1),
            ("Meta Pixel çerez onayından önce tetikleniyor", "track.consent.prefire", "Ana sayfa", Severity.Critical, FindingStatus.Open, 1),
            ("Ürün sayfasında GA4 iki kez yükleniyor", "track.tag.duplicate", "Ürün", Severity.High, FindingStatus.Open, 8420),
            ("Ürün sayfalarında LCP görseli lazy yükleniyor", "perf.lcp.lazy", "Ürün", Severity.High, FindingStatus.Reopened, 7578),
            ("2.430 sayfada title tekrarlanıyor", "seo.title.duplicate", "Ürün", Severity.High, FindingStatus.Open, 2430),
            ("1.232 sayfada HTML sıkıştırılmadan gönderiliyor", "perf.compression.missing", "Araç uyumluluk", Severity.High, FindingStatus.Open, 1232),
            ("Statik dosyaların önbellek süresi çok kısa", "perf.cache.static", "Ürün", Severity.Medium, FindingStatus.Open, 8420),
            ("618 sayfada içerik eşiğin altında", "content.thin", "Lastik ölçü", Severity.Medium, FindingStatus.Snoozed, 618),
            ("40 sayfada meta description eksik", "seo.meta.description.missing", "Jant", Severity.Medium, FindingStatus.Resolved, 40),
            ("6.420 sayfada BreadcrumbList verisi yok", "seo.schema.breadcrumb.missing", "Ürün", Severity.Low, FindingStatus.FalsePositive, 6420)
        ];
        return samples
            .Select((s, i) => new DesignFindingRow(s.Title, s.Code, s.PageType, s.Severity, s.Status, s.Pages, now.AddHours(-3 - i * 7)))
            .ToList();
    }

    private static List<DesignOrderDay> SampleOrders(DateOnly today)
    {
        int[] orders = [176, 205, 140, 147, 210, 212, 192, 201, 167, 138, 139, 173, 200, 174];
        double[] ratio = [0.97, 0.98, 0.97, 0.98, 0.98, 0.96, 0.97, 0.97, 0.98, 0.98, 0.97, 0.80, 0.83, 0.84];
        return orders.Select((o, i) => new DesignOrderDay(today.AddDays(i - orders.Length + 1), o, (int)Math.Round(o * ratio[i]))).ToList();
    }

    private static List<DesignTrendPoint> SampleTrend(DateOnly today) =>
        Enumerable.Range(0, 30)
            .Select(i => new DesignTrendPoint(
                today.AddDays(i - 29),
                i >= 27 ? 43 : 57 + i % 3,
                70 + i / 4 + i % 2,
                i is >= 17 and <= 20 ? 58 : 64 + i % 3))
            .ToList();
}
