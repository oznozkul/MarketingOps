using System.Net.Http.Json;
using MarketingOps.Application.Notifications;
using MarketingOps.Domain.Common;
using MarketingOps.Domain.Notifications;

namespace MarketingOps.Infrastructure.Notifications;

public sealed class TeamsNotificationChannel(IHttpClientFactory http) : INotificationChannel
{
    public NotificationChannelKind Kind => NotificationChannelKind.Teams;

    public Task SendAsync(OutboundNotification message, string? secret, CancellationToken cancellationToken) =>
        Post(secret, new { text = Text(message) }, cancellationToken);

    private async Task Post(string? secret, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secret)) throw new DomainException("Webhook adresi yok.");
        using var response = await http.CreateClient("webhooks").PostAsJsonAsync(secret, payload, cancellationToken);
        if (!response.IsSuccessStatusCode) throw new DomainException("Webhook mesajı kabul etmedi.");
    }

    internal static string Text(OutboundNotification message) => message.Href is null ? $"{message.Title}\n{message.Body}" : $"{message.Title}\n{message.Body}\n{message.Href}";
}
