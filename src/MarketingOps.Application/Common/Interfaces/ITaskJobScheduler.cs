namespace MarketingOps.Application.Common.Interfaces;

/// <summary>Görevlerin arka plan işleri (Hangfire). Testte sahtelenebilsin diye arayüz.</summary>
public interface ITaskJobScheduler
{
    /// <summary>Görevin önerisini AI ile zenginleştirme işini kuyruğa ekler. AI ayarlı değilse iş hiçbir şey yapmaz.</summary>
    void EnqueueEnrichment(Guid taskId);
}
