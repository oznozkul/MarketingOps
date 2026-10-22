namespace MarketingOps.Infrastructure.Crawling;

/// <summary>Tarayıcının HTTP katmanı. Testlerde gerçek ağ yerine sahte bir site kullanılabilsin diye arayüzdür.</summary>
public interface ICrawlerHttpClient
{
    /// <summary>Tek istek atar, yönlendirmeyi izlemez. Ağ hatası/zaman aşımı <see cref="HttpRequestException"/> ya da <see cref="TaskCanceledException"/> fırlatır.</summary>
    Task<CrawlerResponse> GetAsync(Uri url, CancellationToken cancellationToken);
}
