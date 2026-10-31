using MarketingOps.Domain.Sites;

namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Arka plan iş kuyruğu (Hangfire). Application'ın kuyruk teknolojisine bağımlı olmaması ve testte sahtelenebilmesi için.</summary>
public interface ICrawlScheduler
{
    void EnqueueCrawl(Guid crawlId);

    /// <summary>Biten taramanın render denetimini kuyruğa ekler; o bitince değerlendirme kuyruğa girer.</summary>
    void EnqueueRender(Guid crawlId);
    /// <summary>Biten taramanın kural değerlendirmesini kuyruğa ekler.</summary>
    void EnqueueEvaluation(Guid crawlId);

    /// <summary>Bağlı connector'ların (Search Console, GA4) senkronunu hemen kuyruğa ekler.</summary>
    void EnqueueConnectorSync(Guid siteId);

    /// <summary>Örnek sayfaların PageSpeed ölçümünü hemen kuyruğa ekler.</summary>
    void EnqueueWebVitals(Guid siteId);

    /// <summary>Sitenin tarama sıklığına göre tekrarlayan tarama işini ve günlük connector senkronunu oluşturur ya da günceller.</summary>
    void ScheduleSite(Guid siteId, CrawlSchedule schedule);

    void RemoveSite(Guid siteId);
}
