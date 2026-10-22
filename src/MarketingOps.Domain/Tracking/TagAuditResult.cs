using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Tracking;

/// <summary>Bir sayfa ve bir satıcı için denetim hücresi. İstekler jsonb olarak saklanır.</summary>
public class TagAuditResult : Entity
{
    public const int LabelMaxLength = 200;

    private List<string> _foundIds = [];
    private List<CapturedTagRequest> _requests = [];

    public Guid AuditRunId { get; private set; }
    public int Sequence { get; private set; }
    public string PageLabel { get; private set; } = null!;
    public Guid? PageTypeId { get; private set; }
    public string Url { get; private set; } = null!;
    public TagVendor Vendor { get; private set; }
    public TagAuditStatus Status { get; private set; }
    public IReadOnlyList<string> FoundIds => _foundIds;
    public IReadOnlyList<CapturedTagRequest> Requests => _requests;

    private TagAuditResult() { }

    public static TagAuditResult Create(
        Guid auditRunId, int sequence, string pageLabel, Guid? pageTypeId, string url, TagVendor vendor, TagAuditStatus status,
        IReadOnlyList<string> foundIds, IReadOnlyList<CapturedTagRequest> requests) => new()
    {
        AuditRunId = auditRunId,
        Sequence = sequence,
        PageLabel = pageLabel.Length <= LabelMaxLength ? pageLabel : pageLabel[..LabelMaxLength],
        PageTypeId = pageTypeId,
        Url = url,
        Vendor = vendor,
        Status = status,
        _foundIds = foundIds.ToList(),
        _requests = requests.ToList()
    };
}
