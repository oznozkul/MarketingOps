using System.Net.Http.Json;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;

namespace MarketingOps.Infrastructure.Notifications;

public sealed class SlackNotificationChannel(IHttpClientFactory http) : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.Slack;

    public async Task SendAsync(OutboundNotification message, string? secret, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secret)) throw new DomainException("Webhook adresi yok.");
        using var response = await http.CreateClient("webhooks").PostAsJsonAsync(secret, new { text = TeamsNotificationChannel.Text(message) }, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new DomainException("Webhook mesajı kabul etmedi.");
    }
}
