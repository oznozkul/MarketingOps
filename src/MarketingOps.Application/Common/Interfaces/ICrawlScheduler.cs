using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Arka plan iş kuyruğu (Hangfire). Application'ın kuyruk teknolojisine bağımlı olmaması ve testte sahtelenebilmesi için.</summary>
public interface ICrawlScheduler
{
    void EnqueueCrawl(Guid crawlId);

    /// <summary>Biten taramanın kural değerlendirmesini kuyruğa ekler.</summary>
    void EnqueueEvaluation(Guid crawlId);

    /// <summary>Sitenin tarama sıklığına göre tekrarlayan işi oluşturur ya da günceller.</summary>
    void ScheduleSite(Guid siteId, CrawlSchedule schedule);

    void RemoveSite(Guid siteId);
}
