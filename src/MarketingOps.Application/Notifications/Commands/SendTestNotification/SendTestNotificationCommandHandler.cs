using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.SendTestNotification;

public sealed class SendTestNotificationCommandHandler(
    IAppDbContext db,
    IEnumerable<INotificationChannel> channels,
    ConnectorSecretProtector protector,
    MailOptions mail) : IRequestHandler<SendTestNotificationCommand, string>
{
    public async Task<string> Handle(SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        var channel = channels.First(c => c.Kind == request.Channel);
        var config = await db.NotificationChannels.AsNoTracking().FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        var secret = request.Channel switch
        {
            NotificationChannelKind.Teams => Unprotect(config?.ProtectedTeamsWebhook),
            NotificationChannelKind.Slack => Unprotect(config?.ProtectedSlackWebhook),
            _ => null
        };
        if (request.Channel == NotificationChannelKind.Email && !mail.IsConfigured)
            throw new DomainException("SMTP yapılandırılmadı.");
        if (request.Channel is NotificationChannelKind.Teams or NotificationChannelKind.Slack && secret is null)
            throw new DomainException("Webhook adresi yok.");

        var emails = request.Channel == NotificationChannelKind.Email
            ? (await db.NotificationRules.AsNoTracking().Where(r => r.SiteId == request.SiteId).ToListAsync(cancellationToken)).SelectMany(r => r.Recipients).Distinct().ToList()
            : [];
        if (request.Channel == NotificationChannelKind.Email && emails.Count == 0)
            throw new DomainException("Test için bir kuralda e-posta alıcısı olmalı.");

        await channel.SendAsync(new OutboundNotification(request.SiteId, NotificationEventType.NewCriticalFinding, "MarketingOps test bildirimi", "Bu bir test mesajıdır. Kanal çalışıyor.", $"/sites/{request.SiteId}/overview", emails, false), secret, cancellationToken);
        if (request.Channel == NotificationChannelKind.InApp)
            await db.SaveChangesAsync(cancellationToken);
        return request.Channel == NotificationChannelKind.InApp ? "Test bildirimi panele yazıldı." : "Test mesajı gönderildi.";
    }

    private string? Unprotect(string? value) => value is null ? null : protector.Unprotect(value);
}
