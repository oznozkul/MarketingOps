using System.Diagnostics;
using System.IO.Compression;
using System.Text;

namespace MarketingOps.Infrastructure.Crawling;

/// <summary>
/// Gerçek HTTP istemcisi. Otomatik yönlendirme ve otomatik açma kapalıdır: zincirin kaydedilmesi ve
/// Content-Encoding başlığının (sıkıştırma kuralı için) görülebilmesi gerekir. Gövde boyutu sınırlandırılır.
/// </summary>
public sealed class CrawlerHttpClient(HttpClient http) : ICrawlerHttpClient
{
    public const string UserAgent = "MarketingOpsBot/1.0 (+https://marketingops.app/bot)";
    private const int MaxPageBytes = 5 * 1024 * 1024;

    // Sitemap protokolü dosya başına 50 MB'a izin verir.
    private const int MaxSitemapBytes = 60 * 1024 * 1024;

    public async Task<CrawlerResponse> GetAsync(Uri url, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,text/plain;q=0.8,*/*;q=0.5");
        request.Headers.TryAddWithoutValidation("Accept-Encoding", "br, gzip, deflate");

        var timer = Stopwatch.StartNew();
        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        var ttfb = (int)timer.ElapsedMilliseconds;

        var contentType = response.Content.Headers.ContentType?.ToString();
        var encoding = response.Content.Headers.ContentEncoding.LastOrDefault();
        var location = response.Headers.Location is { } loc ? (loc.IsAbsoluteUri ? loc : new Uri(url, loc)) : null;

        string? body = null;
        int? size = null;
        var gzipFile = IsGzipFile(url, contentType);
        if ((IsText(contentType) || gzipFile) && (int)response.StatusCode is >= 200 and < 300)
        {
            var xml = gzipFile || contentType?.Contains("xml", StringComparison.OrdinalIgnoreCase) == true;
            var bytes = await ReadLimitedAsync(response, encoding, gzipFile, xml ? MaxSitemapBytes : MaxPageBytes, cancellationToken);
            size = bytes.Length;
            body = Decode(bytes, response.Content.Headers.ContentType?.CharSet);
        }

        var retryAfter = response.Headers.RetryAfter is { } ra
            ? ra.Delta ?? (ra.Date is { } at ? at - DateTimeOffset.UtcNow : null)
            : null;
        return new CrawlerResponse((int)response.StatusCode, location, contentType, encoding,
            response.Headers.CacheControl?.ToString(), body, size, ttfb, retryAfter);
    }

    private static bool IsText(string? contentType) =>
        contentType is null || contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
        || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
        || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase)
        || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
        || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    /// <summary>Sıkıştırılmış sitemap (sitemap.xml.gz): dosyanın kendisi gzip'tir, Content-Encoding'den bağımsız.</summary>
    private static bool IsGzipFile(Uri url, string? contentType) =>
        url.AbsolutePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
        || contentType?.Contains("gzip", StringComparison.OrdinalIgnoreCase) == true;

    private static async Task<byte[]> ReadLimitedAsync(HttpResponseMessage response, string? encoding, bool gzipFile, int maxBytes, CancellationToken ct)
    {
        await using var raw = await response.Content.ReadAsStreamAsync(ct);
        await using Stream transport = encoding?.ToLowerInvariant() switch
        {
            "gzip" => new GZipStream(raw, CompressionMode.Decompress),
            "br" => new BrotliStream(raw, CompressionMode.Decompress),
            "deflate" => new ZLibStream(raw, CompressionMode.Decompress),
            _ => raw
        };
        await using Stream stream = gzipFile ? new GZipStream(transport, CompressionMode.Decompress) : transport;
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length >= maxBytes) break;
        }
        return buffer.ToArray();
    }

    private static string Decode(byte[] bytes, string? charset)
    {
        try
        {
            return (charset is null ? Encoding.UTF8 : Encoding.GetEncoding(charset.Trim('"'))).GetString(bytes);
        }
        catch (ArgumentException)
        {
            return Encoding.UTF8.GetString(bytes);
        }
    }
}
