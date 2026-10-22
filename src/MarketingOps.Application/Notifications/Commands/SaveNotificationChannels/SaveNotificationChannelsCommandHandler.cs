using MarketingOps.Application.Common.Interfaces;
using MarketingOps.Application.Connectors;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace MarketingOps.Application.Notifications.Commands.SaveNotificationChannels;

public sealed class SaveNotificationChannelsCommandHandler(IAppDbContext db, ConnectorSecretProtector protector) : IRequestHandler<SaveNotificationChannelsCommand>
{
    public async Task Handle(SaveNotificationChannelsCommand request, CancellationToken cancellationToken)
    {
        var config = await db.NotificationChannels.FirstOrDefaultAsync(c => c.SiteId == request.SiteId, cancellationToken);
        if (config is null)
        {
            config = NotificationChannelConfig.Create(request.SiteId);
            db.NotificationChannels.Add(config);
        }

        config.SetTeams(Webhook(request.TeamsWebhook, request.RemoveTeams));
        config.SetSlack(Webhook(request.SlackWebhook, request.RemoveSlack));
        await db.SaveChangesAsync(cancellationToken);
    }

    private string? Webhook(string? value, bool remove)
    {
        if (remove) return "";
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new DomainException("Webhook adresi https ile başlamalı.");
        return protector.Protect(trimmed);
    }
}
