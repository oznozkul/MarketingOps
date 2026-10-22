using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Notifications.Commands.SendDueReports;

public sealed class SendDueReportsCommandHandler(
    IAppDbContext db,
    SummaryReportFactory reports,
    ISummaryReportRenderer renderer,
    IEnumerable<INotificationChannel> channels,
    MailOptions mail,
    TimeProvider clock,
    ILogger<SendDueReportsCommandHandler> logger) : IRequestHandler<SendDueReportsCommand>
{
    public async Task Handle(SendDueReportsCommand request, CancellationToken cancellationToken)
    {
        if (!mail.IsConfigured) return;
        var now = clock.GetUtcNow();
        var schedules = await db.ReportSchedules.Where(s => s.Enabled).ToListAsync(cancellationToken);
        var email = channels.First(c => c.Kind == NotificationChannelKind.Email);
        foreach (var schedule in schedules.Where(s => s.IsDue(now)))
        {
            try
            {
                var report = await reports.BuildAsync(schedule.SiteId, schedule.Period, cancellationToken);
                await email.SendAsync(new OutboundNotification(schedule.SiteId, NotificationEventType.ScoreDropped, $"{report.SiteName} · {report.PeriodLabel}", renderer.Render(report), null, schedule.Recipients, true), null, cancellationToken);
                schedule.MarkSent(IstanbulClock.Today(now));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Özet rapor gönderilemedi. Site {SiteId}, dönem {Period}.", schedule.SiteId, schedule.Period);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }
}
