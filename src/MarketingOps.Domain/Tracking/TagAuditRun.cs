using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Tracking;

/// <summary>Bir sitenin tag denetimi. Hücreler denetim bitince yazılır ve değişmez.</summary>
public class TagAuditRun : Entity
{
    public const int ErrorMaxLength = 2000;

    private readonly List<TagAuditResult> _results = [];

    public Guid SiteId { get; private set; }
    public int Number { get; private set; }
    public AuditStatus Status { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public bool ConsentTested { get; private set; }
    public string? Error { get; private set; }
    public string? OrderSourceError { get; private set; }
    public IReadOnlyList<TagAuditResult> Results => _results;

    private TagAuditRun() { }

    public static TagAuditRun Start(Guid siteId, int number, DateTimeOffset now) => new()
    {
        SiteId = siteId,
        Number = number,
        Status = AuditStatus.Running,
        StartedAt = now
    };

    public void Complete(IReadOnlyList<TagAuditResult> results, bool consentTested, DateTimeOffset now)
    {
        if (Status != AuditStatus.Running) throw new DomainException("Yalnızca çalışan denetim tamamlanabilir.");
        _results.Clear();
        _results.AddRange(results);
        ConsentTested = consentTested;
        Status = AuditStatus.Completed;
        FinishedAt = now;
        Error = null;
    }

    public void NoteOrderSource(string? error) =>
        OrderSourceError = error is null ? null : error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;

    public void Fail(string error, DateTimeOffset now)
    {
        if (Status != AuditStatus.Running) return;
        Status = AuditStatus.Failed;
        FinishedAt = now;
        Error = error.Length > ErrorMaxLength ? error[..ErrorMaxLength] : error;
    }
}
