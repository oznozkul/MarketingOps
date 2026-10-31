using Hangfire;
using MarketingOps.Application.Findings.Commands.EvaluateCrawl;
using MediatR;

namespace MarketingOps.Infrastructure.Jobs;

/// <summary>
/// Biten taramanın kural değerlendirmesi. Komut idempotenttir (değerlendirilmiş tarama atlanır); bu yüzden tekrar denenebilir.
/// Aynı tarama için kuyrukta iki iş olabilir (otomatik + "Kuralları çalıştır", ya da yeniden deneme). İkisi aynı anda çalışırsa
/// ikisi de bulguyu "yok" görüp ekler ve benzersiz indeks çakışır; kilitle ikincisi birincinin bitmesini bekler ve atlanır.
/// </summary>
public sealed class EvaluateCrawlJob(ISender sender)
{
    public const string LockResource = "crawl-evaluation:{0}";
    public const int LockTimeoutSeconds = 600;

    [AutomaticRetry(Attempts = 2)]
    [DisableConcurrentExecution(LockResource, LockTimeoutSeconds)]
    public Task RunAsync(Guid crawlId, CancellationToken cancellationToken) => sender.Send(new EvaluateCrawlCommand(crawlId), cancellationToken);
}
