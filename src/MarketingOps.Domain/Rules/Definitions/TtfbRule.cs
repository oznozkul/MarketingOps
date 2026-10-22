using System.Text;
using System.Text.RegularExpressions;
using MarketingOps.Domain.Crawls;
using MarketingOps.Domain.Findings;

using MarketingOps.Domain.WorkTasks;

namespace MarketingOps.Domain.Rules.Definitions;

/// <summary>Sayfa tipi bazında ortalama TTFB. Ortalaması eşiği aşan tiplerde, eşiği aşan sayfalar etkilenen sayfadır.</summary>
public sealed partial class TtfbRule : RuleBase, ISiteRule
{
    public static readonly RuleThreshold MaxMs = new("maxMs", "En yüksek ortalama TTFB", "ms", 800m, 100m, 10000m);

    /// <summary>Görev adımına dönen HTML boyutu. Ayrı bir eşik ayarı değildir; boyut kuralının eşiğinden bağımsızdır.</summary>
    private const int LargeHtmlKilobytes = 200;
    private const int ListedFileCap = 3;

    public override IReadOnlyList<RuleThreshold> Thresholds => [MaxMs];
    public override string Code => "perf.ttfb.high";
    public override string Name => "TTFB yüksek";
    public override string Description => "Sayfa tipi bazında sunucunun ilk bayta kadar geçen yanıt süresini (TTFB) ölçer.";
    public override FindingCategory Category => FindingCategory.Performance;
    public override Severity DefaultSeverity => Severity.High;
    public override RuleScope Scope => RuleScope.Site;
    public override string ReferenceKey => "perf-doc:p36";
    public override string Impact => "TTFB, sayfanın görünür hale gelme süresinin alt sınırını belirler. Sunucu tarafındaki sebep dışarıdan görülemez; öneriler kontrol edilecek hipotezlerdir.";
    protected override string TitleTemplate => "{n} sayfada TTFB yüksek";
    public override Team Team => Team.Backend;
    public override SuggestionTemplate Suggestion => new(SuggestionKind.Steps, "Sunucunun ilk bayta yanıt süresi kısaltılmalı.", "1. Sayfa tipinin HTML'ini sayfa önbelleği ya da CDN ile önbelleğe alın.\n2. Sayfa üretimindeki yavaş veritabanı sorgularını ve dış servis çağrılarını tespit edip iyileştirin.\n3. Sunucu kaynaklarını ve bölgesini (kullanıcılara yakın) kontrol edin.");
    public override string TeamCriterion => "Sayfa tipi için önbellek/sorgu iyileştirmesi canlıya alındı.";

    public ISiteRuleRun Start(RuleContext context) => new Run(Code, context.Threshold(MaxMs), context.Assets);

    /// <summary>
    /// Bulgu satırındaki kanıttan görev adımları. Sinyal yoksa null döner; şablon adımları kalır.
    /// Sayılar cümlede yazılmış ölçümden okunur.
    /// </summary>
    public static string? StepsFromDetail(string? detail)
    {
        if (string.IsNullOrWhiteSpace(detail)) return null;
        var parts = detail.Split("İlk bayttan sonra:", 2, StringSplitOptions.None);
        var page = parts[0];
        var after = parts.Length > 1 ? parts[1] : "";
        var steps = new List<string>();

        var kilobytes = HtmlKilobytes(page);
        var large = kilobytes >= LargeHtmlKilobytes;
        var uncached = page.Contains("Cache-Control yok", StringComparison.Ordinal)
            || page.Contains("no-store", StringComparison.OrdinalIgnoreCase);
        if (large && uncached)
            steps.Add("HTML yanıtı büyük ve önbelleksiz. Sayfa önbelleğini ya da CDN'i ve sayfa üretimini kontrol edin.");
        else if (large)
            steps.Add("HTML yanıtı büyük. Sayfa üretimini ve önbelleği kontrol edin.");
        else if (uncached)
            steps.Add(page.Contains("Cache-Control yok", StringComparison.Ordinal)
                ? "HTML yanıtında Cache-Control yok. Sayfa önbelleğini ya da CDN'i kontrol edin."
                : "HTML yanıtı no-store. Sayfa önbelleğini kontrol edin.");

        if (Count(after, "(head, bloklayan)") + Count(after, "(bloklayan)") >= 2)
            steps.Add("Birden fazla bloklayan script var. Tek dosyada toplayın ya da defer ile yükleyin. Bu süre TTFB'ye dahil değildir.");
        if (Count(after, "(stil)") >= 2)
            steps.Add("Birden fazla stil dosyası var. Tek dosyada toplayın. Bu süre TTFB'ye dahil değildir.");

        return steps.Count == 0 ? null : string.Join('\n', steps.Select((step, i) => $"{i + 1}. {step}"));
    }

    private static int Count(string text, string token)
    {
        var count = 0;
        var from = 0;
        while ((from = text.IndexOf(token, from, StringComparison.Ordinal)) >= 0)
        {
            count++;
            from += token.Length;
        }
        return count;
    }

    private static int? HtmlKilobytes(string pagePart)
    {
        var match = HtmlSizeRegex().Match(pagePart);
        if (!match.Success) return null;
        var digits = match.Groups[1].Value.Replace(".", "", StringComparison.Ordinal);
        return int.TryParse(digits, out var kb) ? kb : null;
    }

    [GeneratedRegex(@"(\d{1,3}(?:\.\d{3})+|\d+) KB")]
    private static partial Regex HtmlSizeRegex();

    private sealed class Run(string ruleCode, decimal maxMs, IReadOnlyList<AssetCheck> assets) : ISiteRuleRun
    {
        // Sayfa tipine uymayanlar da ayrı bir grup olarak değerlendirilir.
        private readonly Dictionary<Guid, List<Observed>> _byType = [];

        public void Observe(PageSnapshot page)
        {
            if (!PageChecks.IsHtml(page) || page.ResponseTimeMs is not { } ms) return;
            var key = page.PageTypeId ?? Guid.Empty;
            if (!_byType.TryGetValue(key, out var pages)) _byType[key] = pages = [];
            var blocking = page.Data.Scripts.Where(s => s.Src is not null && !s.Async && !s.Defer).ToList();
            pages.Add(new Observed(
                page.Url, ms, page.StatusCode, page.ContentType, page.ContentEncoding, page.CacheControl, page.HtmlSizeBytes,
                blocking.Where(s => s.InHead).Select(s => s.Src!).ToList(),
                blocking.Where(s => !s.InHead).Select(s => s.Src!).ToList(),
                page.Data.Stylesheets.ToList()));
        }

        public IEnumerable<RuleViolation> Complete()
        {
            var byUrl = new Dictionary<string, AssetCheck>(StringComparer.OrdinalIgnoreCase);
            foreach (var asset in assets) byUrl.TryAdd(asset.Url, asset);

            foreach (var (key, pages) in _byType)
            {
                var average = pages.Average(p => p.Ms);
                if ((decimal)average <= maxMs) continue;
                var pageTypeId = key == Guid.Empty ? (Guid?)null : key;
                foreach (var page in pages.Where(p => p.Ms > maxMs))
                    yield return new RuleViolation(ruleCode, pageTypeId, page.Url, $"{page.Ms.ToString("N0", Tr)} ms", Describe(page, average, byUrl));
            }
        }

        private static string Describe(Observed page, double average, Dictionary<string, AssetCheck> byUrl)
        {
            var text = new StringBuilder()
                .Append("GET ").Append(page.Url).Append(' ')
                .Append(page.Ms.ToString("N0", Tr)).Append(" ms");
            AppendResponse(text, page.StatusCode, page.ContentType, page.HtmlSizeBytes, page.ContentEncoding, page.CacheControl);
            text.Append(" (tip ortalaması ").Append(average.ToString("N0", Tr)).Append(" ms).");

            var files = new List<string>();
            var scripts = page.HeadScripts.Select(url => (url, Tag: "(head, bloklayan)"))
                .Concat(page.BodyScripts.Select(url => (url, Tag: "(bloklayan)")))
                .ToList();
            if (scripts.Count > 0)
            {
                var listed = string.Join("; ", scripts.Take(ListedFileCap).Select(s => File(s.url, byUrl, s.Tag)));
                files.Add(scripts.Count > ListedFileCap ? $"{scripts.Count.ToString("N0", Tr)} bloklayan script, örnek: {listed}" : listed);
            }
            var styles = page.Stylesheets
                .OrderByDescending(href => byUrl.TryGetValue(href, out var check) ? check.ResponseTimeMs ?? int.MinValue : int.MinValue)
                .Take(ListedFileCap)
                .Select(href => File(href, byUrl, "(stil)"))
                .ToList();
            if (styles.Count > 0)
                files.Add(page.Stylesheets.Count > ListedFileCap ? $"{page.Stylesheets.Count.ToString("N0", Tr)} stil dosyası, örnek: {string.Join("; ", styles)}" : string.Join("; ", styles));

            if (files.Count == 0) return text.ToString();
            text.Append(" İlk bayttan sonra: ").Append(string.Join("; ", files)).Append(". Bu dosya süreleri TTFB'ye dahil değildir.");
            return text.ToString();
        }

        private static string File(string url, Dictionary<string, AssetCheck> byUrl, string tag)
        {
            var prefix = "GET " + Endpoint(url);
            if (!byUrl.TryGetValue(url, out var check) || check.StatusCode is null)
                return $"{prefix} {tag}";
            var text = new StringBuilder(prefix);
            if (check.ResponseTimeMs is { } ms) text.Append(' ').Append(ms.ToString("N0", Tr)).Append(" ms");
            AppendResponse(text, check.StatusCode, check.ContentType, check.SizeBytes, check.ContentEncoding, check.CacheControl);
            text.Append(' ').Append(tag);
            return text.ToString();
        }

        private static void AppendResponse(StringBuilder text, int? status, string? contentType, int? sizeBytes, string? encoding, string? cache)
        {
            if (status is { } code) text.Append(", ").Append(code.ToString(Tr));
            if (Media(contentType) is { } media) text.Append(", ").Append(media);
            if (sizeBytes is { } size) text.Append(", ").Append((size / 1024m).ToString("N0", Tr)).Append(" KB");
            text.Append(", Content-Encoding ").Append(string.IsNullOrWhiteSpace(encoding) ? "yok" : encoding);
            text.Append(", Cache-Control ").Append(string.IsNullOrWhiteSpace(cache) ? "yok" : cache);
        }

        private static string? Media(string? contentType)
        {
            if (string.IsNullOrWhiteSpace(contentType)) return null;
            var semi = contentType.IndexOf(';');
            var media = (semi < 0 ? contentType : contentType[..semi]).Trim();
            return media.Length == 0 ? null : media;
        }

        private static string Endpoint(string url) =>
            Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.PathAndQuery : url;

        private sealed record Observed(
            string Url,
            int Ms,
            int? StatusCode,
            string? ContentType,
            string? ContentEncoding,
            string? CacheControl,
            int? HtmlSizeBytes,
            IReadOnlyList<string> HeadScripts,
            IReadOnlyList<string> BodyScripts,
            IReadOnlyList<string> Stylesheets);
    }
}
