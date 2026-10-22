using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Findings;

/// <summary>
/// Bir kuralın bir sayfa tipinde bulduğu sorun. Aynı kural + aynı sayfa tipi tek bulgudur (parmak izi);
/// sorun tekrar görüldüğünde yeni kayıt açılmaz, bu kayıt güncellenir.
/// </summary>
public class Finding : Entity
{
    /// <summary>Etkilenen sayfaların tamamı değil, bu kadarı saklanır; toplam <see cref="AffectedCount"/>'tadır.</summary>
    public const int MaxOccurrencesKept = 1000;
    public const int ReasonMaxLength = 1000;

    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
    private readonly List<FindingOccurrence> _occurrences = [];
    private readonly List<FindingHistoryEntry> _history = [];

    public Guid SiteId { get; private set; }
    public string RuleCode { get; private set; } = null!;
    public Guid? PageTypeId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public Severity Severity { get; private set; }
    public FindingCategory Category { get; private set; }
    public FindingStatus Status { get; private set; }
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }
    public DateTimeOffset? SnoozedUntil { get; private set; }
    public string? FalsePositiveReason { get; private set; }
    public int AffectedCount { get; private set; }

    /// <summary>"Son taramada yeni" filtresi için: bulgunun açıldığı ya da tekrar açıldığı tarama.</summary>
    public Guid LastOpenedCrawlId { get; private set; }
    public IReadOnlyList<FindingOccurrence> Occurrences => _occurrences;
    public IReadOnlyList<FindingHistoryEntry> History => _history;

    public bool IsActive => Status is FindingStatus.Open or FindingStatus.Reopened;

    private Finding() { }

    public static string ComputeFingerprint(Guid siteId, string ruleCode, Guid? pageTypeId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{siteId}|{ruleCode}|{pageTypeId}"));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static Finding Open(Guid siteId, string ruleCode, Guid? pageTypeId, FindingCategory category, Severity severity,
        string title, int affectedCount, IReadOnlyList<FindingOccurrence> occurrences, CrawlObservation crawl)
    {
        if (string.IsNullOrWhiteSpace(ruleCode)) throw new DomainException("Kural kodu boş olamaz.");
        if (affectedCount < 1) throw new DomainException("Bulgu en az bir sayfayı etkilemeli.");

        var finding = new Finding
        {
            SiteId = siteId,
            RuleCode = ruleCode,
            PageTypeId = pageTypeId,
            Fingerprint = ComputeFingerprint(siteId, ruleCode, pageTypeId),
            Category = category,
            Status = FindingStatus.Open,
            FirstSeenAt = crawl.At,
            LastOpenedCrawlId = crawl.CrawlId
        };
        finding.Refresh(title, severity, affectedCount, occurrences, crawl.At);
        finding.Record(crawl.At, FindingEventType.Opened, $"Tarama #{crawl.CrawlNumber}: ilk kez tespit edildi ({finding.Count()}).", crawl.CrawlNumber);
        finding.Raise(new FindingOpened(finding.Id, siteId, ruleCode, severity));
        return finding;
    }

    /// <summary>Sorun bu taramada görüldü.</summary>
    public void Observe(string title, Severity severity, int affectedCount, IReadOnlyList<FindingOccurrence> occurrences, CrawlObservation crawl)
    {
        if (affectedCount < 1) throw new DomainException("Bulgu en az bir sayfayı etkilemeli.");
        Refresh(title, severity, affectedCount, occurrences, crawl.At);

        switch (Status)
        {
            case FindingStatus.Resolved:
                Status = FindingStatus.Reopened;
                ResolvedAt = null;
                LastOpenedCrawlId = crawl.CrawlId;
                Record(crawl.At, FindingEventType.Reopened, $"Tarama #{crawl.CrawlNumber}: çözülmüştü, yeniden görüldü ({Count()}).", crawl.CrawlNumber);
                Raise(new FindingReopened(Id, SiteId, RuleCode, severity, crawl.CrawlNumber));
                break;
            case FindingStatus.Snoozed when SnoozedUntil <= crawl.At:
                Status = FindingStatus.Open;
                SnoozedUntil = null;
                Record(crawl.At, FindingEventType.SnoozeExpired, $"Tarama #{crawl.CrawlNumber}: erteleme süresi doldu, bulgu yeniden açık ({Count()}).", crawl.CrawlNumber);
                break;
            case FindingStatus.Open or FindingStatus.Reopened:
                Record(crawl.At, FindingEventType.SeenAgain, $"Tarama #{crawl.CrawlNumber}: tekrar görüldü ({Count()}). Yeni kayıt açılmadı, bu bulgu güncellendi.", crawl.CrawlNumber);
                break;
            // Yanlış alarm ve süresi dolmamış erteleme: tarama durumu değiştirmez, yalnızca son görülme güncellenir.
        }
    }

    /// <summary>Kural çalıştı ama sorun bu taramada görülmedi.</summary>
    public void MarkNotObserved(CrawlObservation crawl)
    {
        if (Status == FindingStatus.Snoozed && SnoozedUntil <= crawl.At)
        {
            Status = FindingStatus.Open;
            SnoozedUntil = null;
            Record(crawl.At, FindingEventType.SnoozeExpired, $"Tarama #{crawl.CrawlNumber}: erteleme süresi doldu.", crawl.CrawlNumber);
        }
        if (!IsActive) return;

        // Kısmi taramada sayfaların bir kısmı hiç görülmemiş olabilir; sorunun gerçekten düzeldiği kanıtlanamaz.
        if (crawl.IsPartial) return;

        Status = FindingStatus.Resolved;
        ResolvedAt = crawl.At;
        Record(crawl.At, FindingEventType.Resolved, $"Tarama #{crawl.CrawlNumber}: sorun görülmedi, otomatik olarak çözüldü.", crawl.CrawlNumber);
        Raise(new FindingResolved(Id, SiteId, crawl.CrawlNumber));
    }

    public void MarkFalsePositive(string reason, string actor, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("Yanlış alarm için gerekçe zorunludur.");
        if (Status == FindingStatus.FalsePositive) throw new DomainException("Bulgu zaten yanlış alarm olarak işaretli.");

        Status = FindingStatus.FalsePositive;
        FalsePositiveReason = reason.Trim();
        SnoozedUntil = null;
        Record(now, FindingEventType.MarkedFalsePositive, $"Yanlış alarm olarak işaretlendi; kural bu sayfa tipi için susturuldu. Gerekçe: {FalsePositiveReason}", actor: actor);
    }

    public void RevertFalsePositive(string actor, DateTimeOffset now)
    {
        if (Status != FindingStatus.FalsePositive) throw new DomainException("Yalnızca yanlış alarm olarak işaretli bulgu geri alınabilir.");

        Status = FindingStatus.Open;
        FalsePositiveReason = null;
        Record(now, FindingEventType.FalsePositiveReverted, "Yanlış alarm işareti geri alındı; sonraki tarama durumu yeniden belirleyecek.", actor: actor);
    }

    public void Snooze(DateTimeOffset until, string actor, DateTimeOffset now)
    {
        if (until <= now) throw new DomainException("Erteleme tarihi gelecekte olmalı.");
        if (Status is FindingStatus.FalsePositive or FindingStatus.Resolved)
            throw new DomainException("Çözülmüş ya da yanlış alarm olarak işaretli bulgu ertelenemez.");

        Status = FindingStatus.Snoozed;
        SnoozedUntil = until;
        Record(now, FindingEventType.Snoozed, $"{until.ToString("d MMMM yyyy", Tr)} tarihine ertelendi.", actor: actor);
    }

    private void Refresh(string title, Severity severity, int affectedCount, IReadOnlyList<FindingOccurrence> occurrences, DateTimeOffset at)
    {
        Title = title;
        Severity = severity;
        AffectedCount = affectedCount;
        LastSeenAt = at;
        _occurrences.Clear();
        _occurrences.AddRange(occurrences.Take(MaxOccurrencesKept));
    }

    private string Count() => $"{AffectedCount.ToString("N0", Tr)} sayfa";

    private void Record(DateTimeOffset at, FindingEventType type, string description, int? crawlNumber = null, string? actor = null) =>
        _history.Add(new FindingHistoryEntry(at, type, description, crawlNumber, actor));
}
