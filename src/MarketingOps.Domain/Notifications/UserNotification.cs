using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>Panel içi bildirim. Kimlik doğrulama olmadığı için siteye aittir, kullanıcıya değil.</summary>
public class UserNotification : Entity
{
    public const int TitleMaxLength = 200;
    public const int BodyMaxLength = 1000;
    public const int HrefMaxLength = 500;

    public Guid SiteId { get; private set; }
    public NotificationEventType EventType { get; private set; }
    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public string? Href { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }

    private UserNotification() { }

    public static UserNotification Create(Guid siteId, NotificationEventType eventType, string title, string body, string? href, DateTimeOffset createdAt)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Bildirim başlığı boş olamaz.");
        return new UserNotification
        {
            SiteId = siteId,
            EventType = eventType,
            Title = Trim(title, TitleMaxLength),
            Body = Trim(body, BodyMaxLength),
            Href = string.IsNullOrWhiteSpace(href) ? null : Trim(href, HrefMaxLength),
            CreatedAt = createdAt
        };
    }

    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;

    private static string Trim(string value, int max) => value.Trim().Length <= max ? value.Trim() : value.Trim()[..max];
}
