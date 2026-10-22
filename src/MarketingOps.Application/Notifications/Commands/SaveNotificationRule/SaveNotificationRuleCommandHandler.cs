using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.SaveNotificationRule;

public sealed class SaveNotificationRuleCommandHandler(IAppDbContext db) : IRequestHandler<SaveNotificationRuleCommand>
{
    public async Task Handle(SaveNotificationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await db.NotificationRules.FirstOrDefaultAsync(r => r.SiteId == request.SiteId && r.EventType == request.EventType, cancellationToken);
        var recipients = string.IsNullOrWhiteSpace(request.Recipients) ? null : request.Recipients.Split('\n');
        if (rule is null)
            db.NotificationRules.Add(NotificationRule.Create(request.SiteId, request.EventType, request.Enabled, request.Channels, recipients, request.QuietStart, request.QuietEnd, request.RepeatIntervalHours, request.ScoreDropPoints));
        else
            rule.Update(request.Enabled, request.Channels, recipients, request.QuietStart, request.QuietEnd, request.RepeatIntervalHours, request.ScoreDropPoints);
        await db.SaveChangesAsync(cancellationToken);
    }
}
