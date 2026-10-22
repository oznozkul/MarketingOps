using MarketingOps.Domain.Common;
using MarketingOps.Domain.Rules;

namespace MarketingOps.Domain.Crawls;

/// <summary>
/// Bir sitenin belirli bir zamandaki taraması. Durum yalnızca metotlarla değişir; geçersiz geçiş (ör. tamamlanmış
/// taramayı yeniden başlatmak) DomainException fırlatır. Site başına tek aktif tarama kuralı handler'da ve veritabanında korunur.
/// </summary>
public class Crawl : Entity
{
    /// <summary>Hata alan sayfa oranı bunu aşarsa tarama kısmi sayılır; kısmi tarama bulguyu "çözüldü" yapamaz.</summary>
    public const double PartialFailureRatio = 0.1;

    /// <summary>Atlanan URL'lerin tamamı değil, inceleme için bir örneği saklanır.</summary>
    public const int MaxSkippedUrlsKept = 500;

    private readonly List<CrawlStageTiming> _stages = [];
    private readonly List<SkippedUrl> _skippedUrls = [];
    private readonly List<AssetCheck> _assetChecks = [];

    public Guid SiteId { get; private set; }
    public int Number { get; private set; }
    public CrawlStatus Status { get; private set; }
    public CrawlTrigger TriggeredBy { get; private set; }
    public DateTimeOffset QueuedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public int PagesDiscovered { get; private set; }
    public int PagesCrawled { get; private set; }
    public int PagesFailed { get; private set; }
    public int SkippedCount { get; private set; }
    public string? ErrorMessage { get; private set; }
    public IReadOnlyList<CrawlStageTiming> Stages => _stages;
    public IReadOnlyList<SkippedUrl> SkippedUrls => _skippedUrls;

    /// <summary>Örneklenen statik dosyaların yanıt başlıkları (önbellek kuralı için).</summary>
    public IReadOnlyList<AssetCheck> AssetChecks => _assetChecks;

    /// <summary>Kurallar bu tarama üzerinde çalıştırıldı mı; bir tarama yalnızca bir kez değerlendirilir.</summary>
    public DateTimeOffset? EvaluatedAt { get; private set; }
    public int NewFindings { get; private set; }
    public int ResolvedFindings { get; private set; }
    public int ReopenedFindings { get; private set; }

    public bool IsPartial => Status == CrawlStatus.PartiallyCompleted;
    public bool CanBeEvaluated => Status is CrawlStatus.Completed or CrawlStatus.PartiallyCompleted && EvaluatedAt is null;

    public bool IsActive => Status is CrawlStatus.Queued or CrawlStatus.Running;

    private Crawl() { }

    /// <summary>
    /// Sahipliği doğrulanmamış sitede zamanlanmış tarama başlatılmaz: sistem, sahibi olduğu kanıtlanmamış bir siteye
    /// kendiliğinden düzenli istek göndermemeli. Manuel tarama kullanıcının bilinçli eylemidir.
    /// </summary>
    public static Crawl Queue(Guid siteId, bool siteOwnershipVerified, int number, CrawlTrigger trigger, DateTimeOffset now)
    {
        if (number < 1) throw new DomainException("Tarama numarası 1 veya daha büyük olmalı.");
        if (trigger == CrawlTrigger.Schedule && !siteOwnershipVerified)
            throw new DomainException("Sahipliği doğrulanmamış sitede zamanlanmış tarama çalışmaz.");
        return new Crawl { SiteId = siteId, Number = number, TriggeredBy = trigger, Status = CrawlStatus.Queued, QueuedAt = now };
    }

    public void Start(DateTimeOffset now)
    {
        EnsureStatus("başlatılamaz", CrawlStatus.Queued);
        Status = CrawlStatus.Running;
        StartedAt = now;
    }

    /// <summary>Önceki aşamayı kapatır ve yenisini açar.</summary>
    public void EnterStage(CrawlStage stage, DateTimeOffset now)
    {
        EnsureStatus("aşamaya geçemez", CrawlStatus.Running);
        CloseOpenStage(now);
        _stages.Add(new CrawlStageTiming(stage, now, null));
    }

    public void ReportProgress(int discovered, int crawled, int failed)
    {
        EnsureStatus("ilerleme bildiremez", CrawlStatus.Running);
        if (discovered < 0 || crawled < 0 || failed < 0)
            throw new DomainException("İlerleme değerleri negatif olamaz.");
        PagesDiscovered = discovered;
        PagesCrawled = crawled;
        PagesFailed = failed;
    }

    public void RecordSkipped(string url, SkipReason reason)
    {
        SkippedCount++;
        if (_skippedUrls.Count < MaxSkippedUrlsKept) _skippedUrls.Add(new SkippedUrl(url, reason));
    }

    public void RecordAssetCheck(AssetCheck check)
    {
        EnsureStatus("dosya kontrolü kaydedemez", CrawlStatus.Running);
        _assetChecks.Add(check);
    }

    /// <summary>Kural değerlendirmesinin sonucu. Tarama bitmiş olmalı; aşama süresi de kaydedilir.</summary>
    public void RecordEvaluation(DateTimeOffset startedAt, DateTimeOffset finishedAt, int opened, int resolved, int reopened)
    {
        if (!CanBeEvaluated) throw new DomainException("Bu tarama değerlendirilemez ya da zaten değerlendirildi.");
        _stages.Add(new CrawlStageTiming(CrawlStage.RuleEvaluation, startedAt, finishedAt));
        EvaluatedAt = finishedAt;
        NewFindings = opened;
        ResolvedFindings = resolved;
        ReopenedFindings = reopened;
    }

    /// <summary>Hata oranı eşiği aşarsa kısmi, aksi halde tamamlandı olarak kapanır.</summary>
    public void Finish(DateTimeOffset now)
    {
        EnsureStatus("bitirilemez", CrawlStatus.Running);
        CloseOpenStage(now);
        var attempted = PagesCrawled + PagesFailed;
        Status = attempted > 0 && PagesFailed > attempted * PartialFailureRatio ? CrawlStatus.PartiallyCompleted : CrawlStatus.Completed;
        FinishedAt = now;
    }

    public void Fail(string errorMessage, DateTimeOffset now)
    {
        EnsureStatus("hata ile kapatılamaz", CrawlStatus.Queued, CrawlStatus.Running);
        CloseOpenStage(now);
        Status = CrawlStatus.Failed;
        ErrorMessage = string.IsNullOrWhiteSpace(errorMessage) ? "Bilinmeyen hata." : errorMessage.Trim();
        FinishedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureStatus("iptal edilemez", CrawlStatus.Queued, CrawlStatus.Running);
        CloseOpenStage(now);
        Status = CrawlStatus.Cancelled;
        FinishedAt = now;
    }

    private void CloseOpenStage(DateTimeOffset now)
    {
        var last = _stages.Count - 1;
        if (last >= 0 && _stages[last].FinishedAt is null)
            _stages[last] = _stages[last] with { FinishedAt = now };
    }

    private void EnsureStatus(string action, params CrawlStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new DomainException($"{StatusText(Status)} durumundaki tarama {action}.");
    }

    private static string StatusText(CrawlStatus status) => status switch
    {
        CrawlStatus.Queued => "Kuyrukta",
        CrawlStatus.Running => "Çalışan",
        CrawlStatus.Completed => "Tamamlanmış",
        CrawlStatus.PartiallyCompleted => "Kısmen tamamlanmış",
        CrawlStatus.Failed => "Hata almış",
        _ => "İptal edilmiş"
    };
}
