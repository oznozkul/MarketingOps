using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>Bir olayın hangi kanallara gideceği. Site ve olay çifti tektir.</summary>
public class NotificationRule : Entity
{
    public const int MinRepeatHours = 1;
    public const int MaxRepeatHours = 168;

    private List<NotificationChannelKind> _channels = [];
    private List<string> _recipients = [];

    public Guid SiteId { get; private set; }
    public NotificationEventType EventType { get; private set; }
    public bool Enabled { get; private set; }
    public TimeOnly? QuietStart { get; private set; }
    public TimeOnly? QuietEnd { get; private set; }
    public int RepeatIntervalHours { get; private set; }
    public int? ScoreDropPoints { get; private set; }
    public IReadOnlyList<NotificationChannelKind> Channels => _channels;
    public IReadOnlyList<string> Recipients => _recipients;

    public QuietHours QuietHours => new(QuietStart, QuietEnd);

    private NotificationRule() { }

    public static NotificationRule Create(Guid siteId, NotificationEventType eventType, bool enabled,
        IEnumerable<NotificationChannelKind> channels, IEnumerable<string>? recipients,
        TimeOnly? quietStart, TimeOnly? quietEnd, int repeatIntervalHours, int? scoreDropPoints)
    {
        var rule = new NotificationRule { SiteId = siteId, EventType = eventType };
        rule.Update(enabled, channels, recipients, quietStart, quietEnd, repeatIntervalHours, scoreDropPoints);
        return rule;
    }

    public void Update(bool enabled, IEnumerable<NotificationChannelKind> channels, IEnumerable<string>? recipients,
        TimeOnly? quietStart, TimeOnly? quietEnd, int repeatIntervalHours, int? scoreDropPoints)
    {
        var quiet = QuietHours.Create(quietStart, quietEnd);
        if (repeatIntervalHours is < MinRepeatHours or > MaxRepeatHours)
            throw new DomainException($"Tekrar aralığı {MinRepeatHours} ile {MaxRepeatHours} saat arasında olmalı.");
        if (EventType == NotificationEventType.ScoreDropped)
        {
            if (scoreDropPoints is null or < 1 or > 100)
                throw new DomainException("Skor düşüş eşiği 1 ile 100 arasında olmalı.");
        }
        else scoreDropPoints = null;

        var distinct = channels.Distinct().ToList();
        var emails = NotificationAddress.Normalize(recipients);
        if (enabled && distinct.Count == 0) throw new DomainException("Açık bir kuralın en az bir kanalı olmalı.");
        if (distinct.Contains(NotificationChannelKind.Email) && emails.Count == 0)
            throw new DomainException("E-posta kanalı için en az bir alıcı gerekli.");

        Enabled = enabled;
        QuietStart = quiet.Start;
        QuietEnd = quiet.End;
        RepeatIntervalHours = repeatIntervalHours;
        ScoreDropPoints = scoreDropPoints;
        _channels.Clear();
        _channels.AddRange(distinct);
        _recipients.Clear();
        _recipients.AddRange(emails);
    }
}
