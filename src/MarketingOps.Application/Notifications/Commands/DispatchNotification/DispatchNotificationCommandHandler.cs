using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Domain.Connectors;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketingOps.Application.Notifications.Commands.DispatchNotification;

public sealed class DispatchNotificationCommandHandler(
    IAppDbContext db,
    IEnumerable<INotificationChannel> channels,
    ConnectorSecretProtector protector,
    INotificationJobScheduler jobs,
    MailOptions mail,
    TimeProvider clock,
    ILogger<DispatchNotificationCommandHandler> logger) : IRequestHandler<DispatchNotificationCommand>
{
    public async Task Handle(DispatchNotificationCommand request, CancellationToken cancellationToken)
    {
        var rules = await db.NotificationRules
            .Where(r => r.SiteId == request.SiteId && r.EventType == request.EventType && r.Enabled)
            .ToListAsync(cancellationToken);
        if (request.EventType == NotificationEventType.ScoreDropped)
            rules = rules.Where(r => request.Magnitude >= r.ScoreDropPoints).ToList();
        if (rules.Count == 0) return;

        var now = clock.GetUtcNow();
        var findingTitle = request.FindingId is Guid findingId
            ? await db.Findings.AsNoTracking().Where(f => f.Id == findingId).Select(f => f.Title).FirstOrDefaultAsync(cancellationToken)
            : null;
        if (request.FindingId is not null && findingTitle is null) return;

        var (title, body) = NotificationText.Compose(request.EventType, findingTitle, request.Magnitude, request.Detail);
        var href = NotificationText.Href(request.SiteId, request.EventType, request.FindingId);
        var config = await db.NotificationChannels.AsNoTracking().FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        var byKind = channels.ToDictionary(c => c.Kind);
        var failed = false;

        foreach (var rule in rules)
        {
            var delay = rule.QuietHours.DelayUntil(now);
            if (delay is not null)
            {
                jobs.Schedule(request.SiteId, request.EventType, request.FindingId, request.Magnitude, request.Detail, delay.Value);
                continue;
            }

            foreach (var kind in rule.Channels)
            {
                if (!byKind.TryGetValue(kind, out var channel)) continue;
                if (kind == NotificationChannelKind.Email && !mail.IsConfigured) continue;
                var secret = Secret(kind, config);
                if (kind is NotificationChannelKind.Teams or NotificationChannelKind.Slack && secret is null) continue;
                if (!await CooldownAllows(request, kind, rule.RepeatIntervalHours, now, cancellationToken)) continue;

                try
                {
                    await channel.SendAsync(new OutboundNotification(request.SiteId, request.EventType, title, body, href, rule.Recipients, false), secret, cancellationToken);
                    db.NotificationDeliveries.Add(NotificationDelivery.Record(request.SiteId, request.EventType, request.FindingId, kind, now));
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed = true;
                    logger.LogWarning(ex, "Bildirim gönderilemedi. Site {SiteId}, olay {Event}, kanal {Channel}.", request.SiteId, request.EventType, kind);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        if (failed) throw new InvalidOperationException("Bildirimin bir kanalı gönderilemedi.");
    }

    private async Task<bool> CooldownAllows(DispatchNotificationCommand request, NotificationChannelKind kind, int hours, DateTimeOffset now, CancellationToken ct)
    {
        var last = await db.NotificationDeliveries.AsNoTracking()
            .Where(d => d.SiteId == request.SiteId && d.EventType == request.EventType && d.Channel == kind && d.FindingId == request.FindingId)
            .OrderByDescending(d => d.SentAt)
            .Select(d => (DateTimeOffset?)d.SentAt)
            .FirstOrDefaultAsync(ct);
        return NotificationCooldown.Allows(last, now, hours);
    }

    private string? Secret(NotificationChannelKind kind, NotificationChannelConfig? config)
    {
        var protectedValue = kind switch
        {
            NotificationChannelKind.Teams => config?.ProtectedTeamsWebhook,
            NotificationChannelKind.Slack => config?.ProtectedSlackWebhook,
            _ => null
        };
        return protectedValue is null ? null : protector.Unprotect(protectedValue);
    }
}
