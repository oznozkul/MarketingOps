using MarketingOps.Application.Notifications.Commands.SaveNotificationChannels;
using MarketingOps.Application.Notifications.Queries.GetNotificationSettings;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarketingOps.Web.ViewModels.Notifications;

public sealed class NotificationChannelsForm
{
    public string? TeamsWebhook { get; set; }
    public string? SlackWebhook { get; set; }
    public bool RemoveTeams { get; set; }
    public bool RemoveSlack { get; set; }

    [BindNever] public bool HasTeams { get; set; }
    [BindNever] public bool HasSlack { get; set; }
    [BindNever] public bool MailConfigured { get; set; }

    /// <summary>Webhook adresleri tekrar gösterilmez; form yalnızca kayıtlı olup olmadıklarını bilir.</summary>
    public static NotificationChannelsForm From(NotificationSettingsResponse settings) => new()
    {
        HasTeams = settings.HasTeams,
        HasSlack = settings.HasSlack,
        MailConfigured = settings.MailConfigured
    };

    public SaveNotificationChannelsCommand ToCommand(Guid siteId) => new(siteId, TeamsWebhook, SlackWebhook, RemoveTeams, RemoveSlack);
}
