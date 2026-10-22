using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Queries.GetNotificationSettings;

public sealed class GetNotificationSettingsQueryHandler(IAppDbContext db, MailOptions mail) : IRequestHandler<GetNotificationSettingsQuery, NotificationSettingsResponse>
{
    public async Task<NotificationSettingsResponse> Handle(GetNotificationSettingsQuery request, CancellationToken cancellationToken)
    {
        var config = await db.NotificationChannels.AsNoTracking().FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        var rules = await db.NotificationRules.AsNoTracking().Where(r => r.SiteId == request.SiteId).ToListAsync(cancellationToken);
        var reports = await db.ReportSchedules.AsNoTracking().Where(r => r.SiteId == request.SiteId).ToListAsync(cancellationToken);

        var ruleRows = Enum.GetValues<NotificationEventType>().Select(type =>
        {
            var rule = rules.FirstOrDefault(r => r.EventType == type);
            return rule is null
                ? new NotificationRuleResponse(type, false, [], "", null, null, 24, type == NotificationEventType.ScoreDropped ? 10 : null)
                : new NotificationRuleResponse(type, rule.Enabled, rule.Channels, string.Join('\n', rule.Recipients), rule.QuietStart, rule.QuietEnd, rule.RepeatIntervalHours, rule.ScoreDropPoints);
        }).ToList();

        var reportRows = Enum.GetValues<ReportPeriod>().Select(period =>
        {
            var schedule = reports.FirstOrDefault(r => r.Period == period);
            return schedule is null
                ? new ReportScheduleResponse(period, false, new TimeOnly(8, 0), "")
                : new ReportScheduleResponse(period, schedule.Enabled, schedule.SendAt, string.Join('\n', schedule.Recipients));
        }).ToList();

        return new NotificationSettingsResponse(config?.HasTeams ?? false, config?.HasSlack ?? false, mail.IsConfigured, ruleRows, reportRows);
    }
}
