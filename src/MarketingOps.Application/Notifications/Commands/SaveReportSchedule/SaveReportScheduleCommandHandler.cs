using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.SaveReportSchedule;

public sealed class SaveReportScheduleCommandHandler(IAppDbContext db) : IRequestHandler<SaveReportScheduleCommand>
{
    public async Task Handle(SaveReportScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await db.ReportSchedules.FirstOrDefaultAsync(s => s.SiteId == request.SiteId && s.Period == request.Period, cancellationToken);
        var recipients = string.IsNullOrWhiteSpace(request.Recipients) ? null : request.Recipients.Split('\n');
        if (schedule is null)
            db.ReportSchedules.Add(ReportSchedule.Create(request.SiteId, request.Period, request.Enabled, request.SendAt, recipients));
        else
            schedule.Update(request.Enabled, request.SendAt, recipients);
        await db.SaveChangesAsync(cancellationToken);
    }
}
