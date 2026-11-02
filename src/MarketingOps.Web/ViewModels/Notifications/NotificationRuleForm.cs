using MarketingOps.Application.Notifications.Commands.SaveNotificationRule;
using MarketingOps.Application.Notifications.Queries.GetNotificationSettings;
using MarketingOps.Domain.Notifications;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarketingOps.Web.ViewModels.Notifications;

public sealed class NotificationRuleForm
{
    [BindNever] public NotificationEventType EventType { get; set; }
    public bool Enabled { get; set; }
    public List<NotificationChannelKind> Channels { get; set; } = [];
    public string? Recipients { get; set; }
    public TimeOnly? QuietStart { get; set; }
    public TimeOnly? QuietEnd { get; set; }
    public int RepeatIntervalHours { get; set; }
    public int? ScoreDropPoints { get; set; }

    /// <summary>Kanal kutularının yanında "bağlı değil" uyarısı için; kayıtlı kanallardan gelir, formdan okunmaz.</summary>
    [BindNever] public NotificationChannelsForm Available { get; set; } = new();

    public static NotificationRuleForm From(NotificationRuleResponse rule, NotificationChannelsForm available) => new()
    {
        EventType = rule.EventType,
        Enabled = rule.Enabled,
        Channels = rule.Channels.ToList(),
        Recipients = rule.Recipients,
        QuietStart = rule.QuietStart,
        QuietEnd = rule.QuietEnd,
        RepeatIntervalHours = rule.RepeatIntervalHours,
        ScoreDropPoints = rule.ScoreDropPoints,
        Available = available
    };

    public SaveNotificationRuleCommand ToCommand(Guid siteId) =>
        new(siteId, EventType, Enabled, Channels, Recipients, QuietStart, QuietEnd, RepeatIntervalHours, ScoreDropPoints);
}
