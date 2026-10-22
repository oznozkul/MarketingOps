using MarketingOps.Domain.Common;

namespace MarketingOps.Domain.Notifications;

/// <summary>Site başına webhook adresleri. E-posta alıcıları kuraldadır; SMTP uygulama ayarındadır.</summary>
public class NotificationChannelConfig : Entity
{
    public Guid SiteId { get; private set; }
    public string? ProtectedTeamsWebhook { get; private set; }
    public string? ProtectedSlackWebhook { get; private set; }

    public bool HasTeams => ProtectedTeamsWebhook is not null;
    public bool HasSlack => ProtectedSlackWebhook is not null;

    private NotificationChannelConfig() { }

    public static NotificationChannelConfig Create(Guid siteId) => new() { SiteId = siteId };

    /// <summary>null, kayıtlı adresi olduğu gibi bırakır. Boş metin adresi siler.</summary>
    public void SetTeams(string? protectedWebhook)
    {
        if (protectedWebhook is null) return;
        ProtectedTeamsWebhook = protectedWebhook.Length == 0 ? null : protectedWebhook;
    }

    public void SetSlack(string? protectedWebhook)
    {
        if (protectedWebhook is null) return;
        ProtectedSlackWebhook = protectedWebhook.Length == 0 ? null : protectedWebhook;
    }
}
