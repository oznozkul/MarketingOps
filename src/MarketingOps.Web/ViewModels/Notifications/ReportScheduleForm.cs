using MarketingOps.Application.Notifications.Commands.SaveReportSchedule;
using MarketingOps.Application.Notifications.Queries.GetNotificationSettings;
using MarketingOps.Domain.Notifications;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace MarketingOps.Web.ViewModels.Notifications;

public sealed class ReportScheduleForm
{
    [BindNever] public ReportPeriod Period { get; set; }
    public bool Enabled { get; set; }
    public TimeOnly SendAt { get; set; }
    public string? Recipients { get; set; }

    public static ReportScheduleForm From(ReportScheduleResponse schedule) => new()
    {
        Period = schedule.Period,
        Enabled = schedule.Enabled,
        SendAt = schedule.SendAt,
        Recipients = schedule.Recipients
    };

    public SaveReportScheduleCommand ToCommand(Guid siteId) => new(siteId, Period, Enabled, SendAt, Recipients);
}
