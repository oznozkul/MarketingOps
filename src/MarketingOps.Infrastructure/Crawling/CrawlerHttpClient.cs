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
    private const int MaxBodyBytes = 5 * 1024 * 1024;

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
        if (IsText(contentType) && (int)response.StatusCode is >= 200 and < 300)
        {
            var bytes = await ReadLimitedAsync(response, encoding, cancellationToken);
            size = bytes.Length;
            body = Decode(bytes, response.Content.Headers.ContentType?.CharSet);
        }

        return new CrawlerResponse((int)response.StatusCode, location, contentType, encoding,
            response.Headers.CacheControl?.ToString(), body, size, ttfb);
    }

    private static bool IsText(string? contentType) =>
        contentType is null || contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
        || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    private static async Task<byte[]> ReadLimitedAsync(HttpResponseMessage response, string? encoding, CancellationToken ct)
    {
        await using var raw = await response.Content.ReadAsStreamAsync(ct);
        await using Stream stream = encoding?.ToLowerInvariant() switch
        {
            "gzip" => new GZipStream(raw, CompressionMode.Decompress),
            "br" => new BrotliStream(raw, CompressionMode.Decompress),
            "deflate" => new ZLibStream(raw, CompressionMode.Decompress),
            _ => raw
        };
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(chunk, ct)) > 0)
        {
            buffer.Write(chunk, 0, read);
            if (buffer.Length >= MaxBodyBytes) break;
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
